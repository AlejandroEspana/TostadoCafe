using System;
using UnityEngine;
using UnityEngine.Events;

namespace ToastedVr.RoastSimulation
{
    [Serializable]
    public sealed class RoastTextEvent : UnityEvent<string>
    {
    }

    // Eventos para conectar desde el Inspector lo que se ve en la tostadora (granos, humo, sonidos, partículas).
    // Solo escucha a la estación: no cambia nada del tueste.
    public sealed class RoastStationVisualEvents : MonoBehaviour
    {
        [SerializeField] private RoastStationController controller;

        [Header("Station")]
        [SerializeField] private UnityEvent poweredOn = new UnityEvent();
        [SerializeField] private UnityEvent poweredOff = new UnityEvent();

        [Header("Roast")]
        [SerializeField] private UnityEvent charged = new UnityEvent();
        [SerializeField] private UnityEvent turningPointReached = new UnityEvent();
        [SerializeField] private UnityEvent firstCrack = new UnityEvent();
        [SerializeField] private UnityEvent secondCrack = new UnityEvent();
        [Tooltip("Receives grainState: DRYING, MAILLARD, FIRST_CRACK, DARK, SECOND_CRACK or BURNED.")]
        [SerializeField] private RoastTextEvent grainStateChanged = new RoastTextEvent();

        [Header("Alarms")]
        [SerializeField] private UnityEvent burnWarningStarted = new UnityEvent();
        [SerializeField] private UnityEvent burnWarningEnded = new UnityEvent();
        [SerializeField] private UnityEvent burned = new UnityEvent();

        [Header("Discharge")]
        [Tooltip("Receives the result: PERFECT, RAW, BURNED or BAKED. Empty if the backend sent no session.")]
        [SerializeField] private RoastTextEvent discharged = new RoastTextEvent();

        private readonly RoastVisualEventDetector detector = new RoastVisualEventDetector();

        public UnityEvent PoweredOn => poweredOn;
        public UnityEvent PoweredOff => poweredOff;
        public UnityEvent Charged => charged;
        public UnityEvent TurningPointReached => turningPointReached;
        public UnityEvent FirstCrack => firstCrack;
        public UnityEvent SecondCrack => secondCrack;
        public RoastTextEvent GrainStateChanged => grainStateChanged;
        public UnityEvent BurnWarningStarted => burnWarningStarted;
        public UnityEvent BurnWarningEnded => burnWarningEnded;
        public UnityEvent Burned => burned;
        public RoastTextEvent Discharged => discharged;

        public void Observe(bool isPowered, RoastState state)
        {
            foreach (var visualEvent in detector.Update(isPowered, state))
            {
                Raise(visualEvent);
            }
        }

        public void OnDischarged(RoastSessionSummary session)
        {
            discharged.Invoke(session?.Result ?? string.Empty);
        }

        private void OnEnable()
        {
            controller.Changed += ObserveStation;
            controller.Finished += OnDischarged;
        }

        private void OnDisable()
        {
            controller.Changed -= ObserveStation;
            controller.Finished -= OnDischarged;
        }

        private void ObserveStation() => Observe(controller.IsPowered, controller.State);

        private void Raise(RoastVisualEvent visualEvent)
        {
            switch (visualEvent.Kind)
            {
                case RoastVisualEventKind.PoweredOn: poweredOn.Invoke(); break;
                case RoastVisualEventKind.PoweredOff: poweredOff.Invoke(); break;
                case RoastVisualEventKind.Charged: charged.Invoke(); break;
                case RoastVisualEventKind.TurningPointReached: turningPointReached.Invoke(); break;
                case RoastVisualEventKind.FirstCrack: firstCrack.Invoke(); break;
                case RoastVisualEventKind.SecondCrack: secondCrack.Invoke(); break;
                case RoastVisualEventKind.GrainStateChanged: grainStateChanged.Invoke(visualEvent.Value); break;
                case RoastVisualEventKind.BurnWarningStarted: burnWarningStarted.Invoke(); break;
                case RoastVisualEventKind.BurnWarningEnded: burnWarningEnded.Invoke(); break;
                case RoastVisualEventKind.Burned: burned.Invoke(); break;
            }
        }
    }
}
