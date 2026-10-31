using System;
using System.Collections;
using ToastedVr.Authentication;
using ToastedVr.SessionRefresh;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToastedVr.SessionRefreshBootstrap
{
    // Se crea solo al arrancar y sobrevive a los cambios de escena, así que no hace falta tocar ninguna escena.
    public sealed class SessionRefreshService : MonoBehaviour
    {
        private const string ConfigurationResourcePath = "SessionRefresh/SessionRefreshConfiguration";

        private SessionRefreshConfiguration configuration;
        private SessionRefresher refresher;
        private bool isReturningToLogin;

        public static SessionRefreshService Instance { get; private set; }

        public AuthorizedRequestSender Sender { get; private set; }

        public event Action SessionExpired;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            var loadedConfiguration = Resources.Load<SessionRefreshConfiguration>(ConfigurationResourcePath);
            if (loadedConfiguration == null)
            {
                Debug.LogWarning("[SessionRefresh] Configuration not found; the session will not be renewed.");
                return;
            }

            var host = new GameObject(nameof(SessionRefreshService));
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<SessionRefreshService>();
            Instance.Initialize(loadedConfiguration);
        }

        private void Initialize(SessionRefreshConfiguration loadedConfiguration)
        {
            configuration = loadedConfiguration;
            var store = new UnitySessionTokenStore();
            var api = new SessionRefreshApiClient(configuration.BackendBaseUrl, configuration.RefreshPath, configuration.RequestTimeoutSeconds);
            refresher = new SessionRefresher(store, api, routine => StartCoroutine(routine));
            Sender = new AuthorizedRequestSender(store, refresher);
            Sender.SessionExpired += ExpireSession;
            StartCoroutine(KeepSessionFresh());
        }

        private IEnumerator KeepSessionFresh()
        {
            string trackedAccessToken = null;
            var receivedAt = DateTimeOffset.UtcNow;

            while (true)
            {
                var accessToken = UnitySession.AccessToken;
                if (string.IsNullOrEmpty(accessToken) || isReturningToLogin)
                {
                    trackedAccessToken = null;
                    yield return null;
                    continue;
                }

                if (accessToken != trackedAccessToken)
                {
                    trackedAccessToken = accessToken;
                    receivedAt = DateTimeOffset.UtcNow;
                    LogSchedule(receivedAt);
                }

                if (!IsRefreshDue(receivedAt))
                {
                    yield return null;
                    continue;
                }

                var outcome = SessionRefreshOutcome.Unavailable;
                yield return refresher.Refresh(result => outcome = result);

                if (outcome == SessionRefreshOutcome.Rejected)
                {
                    ExpireSession();
                }
                else if (outcome == SessionRefreshOutcome.Unavailable)
                {
                    LogDiagnostic($"Backend unavailable; retrying in {configuration.UnavailableRetrySeconds} s.");
                    yield return new WaitForSecondsRealtime(configuration.UnavailableRetrySeconds);
                }
            }
        }

        private bool IsRefreshDue(DateTimeOffset receivedAt)
        {
            if (!RefreshSchedule.TryParseExpiry(UnitySession.ExpiresAt, out var expiresAt))
            {
                return false;
            }

            var delay = RefreshSchedule.DelayUntilRefresh(receivedAt, expiresAt, DateTimeOffset.UtcNow, configuration.RefreshMargin);
            return delay == TimeSpan.Zero;
        }

        private void ExpireSession()
        {
            if (isReturningToLogin)
            {
                return;
            }

            isReturningToLogin = true;
            UnitySession.Clear();
            LogDiagnostic($"Refresh rejected; returning to {configuration.LoginSceneName} in {configuration.ReturnToLoginDelaySeconds} s.");
            SessionExpired?.Invoke();
            StartCoroutine(ReturnToLogin());
        }

        private IEnumerator ReturnToLogin()
        {
            yield return new WaitForSecondsRealtime(configuration.ReturnToLoginDelaySeconds);
            SceneManager.LoadScene(configuration.LoginSceneName);
            isReturningToLogin = false;
        }

        // Solo muestra fechas: los tokens nunca van al log.
        private void LogSchedule(DateTimeOffset receivedAt)
        {
            if (!RefreshSchedule.TryParseExpiry(UnitySession.ExpiresAt, out var expiresAt))
            {
                LogDiagnostic("Session expiry unreadable; renewal will happen on the next 401.");
                return;
            }

            var delay = RefreshSchedule.DelayUntilRefresh(receivedAt, expiresAt, receivedAt, configuration.RefreshMargin);
            LogDiagnostic(delay.HasValue
                ? $"Session expires at {expiresAt:HH:mm:ss} UTC; renewing in {delay.Value.TotalSeconds:0} s."
                : "Device clock is ahead of the server; renewal will happen on the next 401.");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void LogDiagnostic(string message)
        {
            Debug.Log($"[SessionRefresh] {message}");
        }
    }
}
