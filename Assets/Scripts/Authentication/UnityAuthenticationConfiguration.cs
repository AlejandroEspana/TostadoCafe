using UnityEngine;

namespace ToastedVr.Authentication
{
    [CreateAssetMenu(fileName = "UnityAuthenticationConfiguration", menuName = "Toasted VR/Unity Authentication Configuration")]
    public sealed class UnityAuthenticationConfiguration : ScriptableObject
    {
        [Tooltip("Backend base address without the Unity login endpoint path. Use HTTPS outside local Editor development.")]
        [SerializeField] private string backendBaseUrl;

        public string BackendBaseUrl => backendBaseUrl;
    }
}
