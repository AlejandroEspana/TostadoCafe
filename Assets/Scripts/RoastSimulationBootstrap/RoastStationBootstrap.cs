using ToastedVr.Authentication;
using ToastedVr.RoastAlerts;
using ToastedVr.RoastSimulation;
using ToastedVr.RoastVoice;
using ToastedVr.SessionRefreshBootstrap;
using UnityEngine;

namespace ToastedVr.RoastSimulationBootstrap
{
    // Une la estación con lo que ya existe: la dirección del backend del login y el envío con token que renueva solo.
    // Si la renovación es rechazada, SessionRefreshService vuelve al login; aquí solo se avisa al jugador.
    public sealed class RoastStationBootstrap : MonoBehaviour
    {
        [SerializeField] private UnityAuthenticationConfiguration authenticationConfiguration;
        [SerializeField] private RoastStationController controller;
        [SerializeField] private VoiceAssistantController voiceAssistant;
        [SerializeField] private RoastAlertsController alerts;

        private SessionRefreshService refreshService;

        private void Start()
        {
            refreshService = SessionRefreshService.Instance;
            if (authenticationConfiguration == null || refreshService == null)
            {
                Debug.LogWarning("[RoastSimulation] Missing backend configuration or session service; the station stays off.");
                return;
            }

            controller.Initialize(authenticationConfiguration.BackendBaseUrl, refreshService.Sender);
            voiceAssistant.Initialize(authenticationConfiguration.BackendBaseUrl, refreshService.Sender);
            alerts.Initialize(authenticationConfiguration.BackendBaseUrl, refreshService.Sender);
            refreshService.SessionExpired += controller.ShowSessionExpired;
        }

        private void OnDestroy()
        {
            if (refreshService != null)
            {
                refreshService.SessionExpired -= controller.ShowSessionExpired;
            }
        }
    }
}
