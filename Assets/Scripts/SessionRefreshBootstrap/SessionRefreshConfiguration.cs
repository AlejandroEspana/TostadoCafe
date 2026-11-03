using System;
using ToastedVr.Authentication;
using UnityEngine;

namespace ToastedVr.SessionRefreshBootstrap
{
    [CreateAssetMenu(fileName = "SessionRefreshConfiguration", menuName = "Toasted VR/Session Refresh Configuration")]
    public sealed class SessionRefreshConfiguration : ScriptableObject
    {
        [Tooltip("Same configuration the login uses, so the backend address lives in one place.")]
        [SerializeField] private UnityAuthenticationConfiguration authenticationConfiguration;
        [SerializeField] private string refreshPath;
        [Tooltip("Seconds before expiry to renew. Tokens shorter than twice this value renew at half their lifetime.")]
        [SerializeField, Min(0f)] private float refreshMarginSeconds;
        [SerializeField, Min(1)] private int requestTimeoutSeconds;
        [Tooltip("Seconds between attempts while the backend does not answer. The player is not sent to the login meanwhile.")]
        [SerializeField, Min(1f)] private float unavailableRetrySeconds;
        [SerializeField] private string loginSceneName;
        [Tooltip("Seconds the expired-session message stays visible before loading the login scene.")]
        [SerializeField, Min(0f)] private float returnToLoginDelaySeconds;

        public string BackendBaseUrl => authenticationConfiguration != null ? authenticationConfiguration.BackendBaseUrl : null;
        public string RefreshPath => refreshPath;
        public TimeSpan RefreshMargin => TimeSpan.FromSeconds(refreshMarginSeconds);
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public float UnavailableRetrySeconds => unavailableRetrySeconds;
        public string LoginSceneName => loginSceneName;
        public float ReturnToLoginDelaySeconds => returnToLoginDelaySeconds;
    }
}
