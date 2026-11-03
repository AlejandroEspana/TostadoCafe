using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ToastedVr.RoastSimulation
{
    // Conecta las perillas físicas con el controlador y solo deja activas las que valen en la fase actual.
    public sealed class RoastStationControls : MonoBehaviour
    {
        [SerializeField] private RoastStationController controller;
        [SerializeField] private XRSimpleInteractable powerButton;
        [SerializeField] private XRSimpleInteractable chargeButton;
        [SerializeField] private XRSimpleInteractable dischargeButton;
        [SerializeField] private GasKnobInteractable gasKnob;
        [SerializeField] private TMP_Text powerLabel;
        [SerializeField] private TMP_Text chargeLabel;
        [SerializeField] private TMP_Text dischargeLabel;
        [SerializeField] private TMP_Text gasLabel;

        private GasSendThrottle gasThrottle;

        private void Awake()
        {
            var configuration = controller.Configuration;
            gasKnob.Initialize(
                new GasKnobMapping(configuration.KnobMinAngle, configuration.KnobMaxAngle, configuration.MinGasPercent, configuration.MaxGasPercent, configuration.GasStepPercent),
                configuration.KnobDeadZoneMeters);
            gasThrottle = new GasSendThrottle(configuration.GasSendIntervalSeconds);
        }

        private void OnEnable()
        {
            powerButton.selectEntered.AddListener(OnPowerPressed);
            chargeButton.selectEntered.AddListener(OnChargePressed);
            dischargeButton.selectEntered.AddListener(OnDischargePressed);
            gasKnob.Grabbed += OnKnobGrabbed;
            gasKnob.Turning += OnKnobTurning;
            gasKnob.Released += OnKnobReleased;
            controller.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            powerButton.selectEntered.RemoveListener(OnPowerPressed);
            chargeButton.selectEntered.RemoveListener(OnChargePressed);
            dischargeButton.selectEntered.RemoveListener(OnDischargePressed);
            gasKnob.Grabbed -= OnKnobGrabbed;
            gasKnob.Turning -= OnKnobTurning;
            gasKnob.Released -= OnKnobReleased;
            controller.Changed -= Refresh;
        }

        private void OnPowerPressed(SelectEnterEventArgs args)
        {
            Debug.Log("[RoastSimulation] Power button selected");
            controller.TogglePower();
        }

        private void OnChargePressed(SelectEnterEventArgs args) => controller.Charge();

        private void OnDischargePressed(SelectEnterEventArgs args) => controller.Discharge();

        private void OnKnobGrabbed(int gas) => gasThrottle.Begin(gas);

        private void OnKnobTurning(int gas)
        {
            ShowGasLabel(gas);
            if (gasThrottle.ShouldSendWhileTurning(gas, Time.unscaledTime))
            {
                controller.RequestGas(gas);
            }
        }

        private void OnKnobReleased(int gas)
        {
            if (gasThrottle.ShouldSendOnRelease(gas, Time.unscaledTime))
            {
                controller.RequestGas(gas);
            }
        }

        private void Refresh()
        {
            var phase = controller.Phase;
            var canAct = controller.IsReady && controller.IsPowered && !controller.IsBusy;

            powerButton.enabled = controller.IsReady && !controller.IsBusy;
            chargeButton.enabled = canAct && RoastPhaseRules.CanCharge(phase);
            dischargeButton.enabled = canAct && RoastPhaseRules.CanDischarge(phase);
            gasKnob.enabled = controller.IsReady && controller.IsPowered && RoastPhaseRules.CanChangeGas(phase);

            var texts = controller.Messages;
            powerLabel.text = controller.IsPowered ? texts.PowerOffLabel : texts.PowerOnLabel;
            chargeLabel.text = texts.ChargeLabel;
            dischargeLabel.text = texts.DischargeLabel;

            if (controller.State != null)
            {
                gasKnob.ShowGas(controller.State.GasPercent);
            }

            ShowGasLabel(gasKnob.CurrentGas);
        }

        private void ShowGasLabel(int gas)
        {
            gasLabel.text = string.Format(controller.Messages.GasKnobFormat, gas);
        }
    }
}
