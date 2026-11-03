using UnityEngine;

namespace ToastedVr.RoastAlerts
{
    [CreateAssetMenu(fileName = "RoastAlertsConfiguration", menuName = "Toasted VR/Roast Alerts Configuration")]
    public sealed class RoastAlertsConfiguration : ScriptableObject
    {
        [Header("Backend")]
        [SerializeField] private string alertsPath;
        [Tooltip("The package carries the audio of every phrase (a few MB); the contract suggests waiting up to 30 s.")]
        [SerializeField, Min(1)] private int requestTimeoutSeconds;

        [Header("Storage")]
        [Tooltip("Folder inside Application.persistentDataPath where the last package and its ETag are kept.")]
        [SerializeField] private string storageFolderName;

        public string AlertsPath => alertsPath;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public string StorageFolderName => storageFolderName;
    }
}
