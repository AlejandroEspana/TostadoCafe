using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToastedVr.Authentication
{
    public sealed class UnityLoginController : MonoBehaviour
    {
        private const string ExperienceSceneName = "urp_scene_with_training";
        private const string EmptyCodeMessage = "Ingresa tu código de acceso.";
        private const string MissingConfigurationMessage = "La dirección del servidor no está configurada.";
        private const string InvalidCodeMessage = "El código de acceso no es válido.";
        private const string ConnectionFailureMessage = "No fue posible conectar con el servidor.";
        private const string TimeoutMessage = "La conexión tardó demasiado. Inténtalo de nuevo.";
        private const string ServerFailureMessage = "El servidor no pudo procesar la solicitud.";
        private const string MalformedResponseMessage = "No fue posible validar la respuesta del servidor.";
        private const string LoadingMessage = "Validando acceso...";
        private const string SuccessMessage = "Acceso validado.";

        [SerializeField] private UnityAuthenticationConfiguration configuration;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private TextMeshProUGUI statusText;

        private UnityLoginApiClient apiClient;
        private Coroutine loginRoutine;
        private bool isLoadingExperience;

        private void Awake()
        {
            apiClient = new UnityLoginApiClient();
        }

        private void Start()
        {
            if (configuration == null || codeInput == null || loginButton == null || statusText == null)
            {
                enabled = false;
                return;
            }

            loginButton.onClick.AddListener(SubmitLogin);
            statusText.text = string.Empty;
        }

        public void SubmitLogin()
        {
            if (loginRoutine != null || isLoadingExperience)
            {
                return;
            }

            var code = NormalizeCode(codeInput.text);
            if (string.IsNullOrWhiteSpace(code))
            {
                statusText.text = EmptyCodeMessage;
                return;
            }

            if (configuration == null || string.IsNullOrWhiteSpace(configuration.BackendBaseUrl))
            {
                statusText.text = MissingConfigurationMessage;
                return;
            }

            loginRoutine = StartCoroutine(Login(code));
        }

        private IEnumerator Login(string code)
        {
            SetLoadingState(true);
            yield return apiClient.Login(configuration.BackendBaseUrl, code, HandleSuccess, ShowFailure);

            if (loginRoutine != null && !isLoadingExperience)
            {
                loginRoutine = null;
                SetLoadingState(false);
            }
        }

        private void HandleSuccess(UnityLoginResponse response)
        {
            UnitySession.SetSession(response);
            codeInput.text = string.Empty;
            statusText.text = SuccessMessage;
            isLoadingExperience = true;
            StartCoroutine(LoadExperienceScene());
        }

        private void ShowFailure(UnityLoginFailure failure)
        {
            statusText.text = failure switch
            {
                UnityLoginFailure.InvalidCode => InvalidCodeMessage,
                UnityLoginFailure.Timeout => TimeoutMessage,
                UnityLoginFailure.Server => ServerFailureMessage,
                UnityLoginFailure.MalformedResponse => MalformedResponseMessage,
                _ => ConnectionFailureMessage
            };
        }

        private void SetLoadingState(bool isLoading)
        {
            codeInput.interactable = !isLoading;
            loginButton.interactable = !isLoading;
            statusText.text = isLoading ? LoadingMessage : statusText.text;
        }

        private static string NormalizeCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var normalizedCode = new StringBuilder(input.Length);
            foreach (var character in input)
            {
                if (!char.IsWhiteSpace(character))
                    normalizedCode.Append(char.ToUpperInvariant(character));
            }

            return normalizedCode.ToString();
        }

        private IEnumerator LoadExperienceScene()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            SceneManager.LoadScene(ExperienceSceneName);
        }

        private void OnDestroy()
        {
            if (loginButton != null)
            {
                loginButton.onClick.RemoveListener(SubmitLogin);
            }

            if (codeInput != null)
            {
                codeInput.text = string.Empty;
            }
        }
    }
}
