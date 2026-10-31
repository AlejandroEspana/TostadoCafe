using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace ToastedVr.RoastSimulation
{
    // Perilla que se agarra y se gira alrededor de su eje Y local. El ángulo sale de la posición de la mano (o del punto
    // del rayo) alrededor del eje, así funciona igual de cerca, de lejos y con el XR Device Simulator moviendo el mouse.
    public sealed class GasKnobInteractable : XRBaseInteractable
    {
        [Tooltip("Visible part that rotates. The root stays still so the angle is measured against a fixed frame.")]
        [SerializeField] private Transform knobVisual;

        private GasKnobMapping mapping;
        private float deadZoneMeters;
        private float knobAngle;
        private float? previousHandAngle;
        private int currentGas;

        public event Action<int> Grabbed;
        public event Action<int> Turning;
        public event Action<int> Released;

        public int CurrentGas => currentGas;

        public void Initialize(GasKnobMapping knobMapping, float deadZone)
        {
            mapping = knobMapping;
            deadZoneMeters = deadZone;
            ShowGas(mapping.GasForAngle(knobAngle));
        }

        // El gas del servidor manda mientras nadie gira la perilla.
        public void ShowGas(int gas)
        {
            if (mapping == null || isSelected)
            {
                return;
            }

            currentGas = gas;
            knobAngle = mapping.AngleForGas(gas);
            ApplyVisual();
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            previousHandAngle = null;
            Grabbed?.Invoke(currentGas);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            previousHandAngle = null;
            Released?.Invoke(currentGas);
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || !isSelected || mapping == null)
            {
                return;
            }

            var handAngle = HandAngle();
            if (!handAngle.HasValue)
            {
                return;
            }

            if (previousHandAngle.HasValue)
            {
                knobAngle = mapping.ClampAngle(knobAngle + Mathf.DeltaAngle(previousHandAngle.Value, handAngle.Value));
            }

            previousHandAngle = handAngle;

            var gas = mapping.GasForAngle(knobAngle);
            if (gas != currentGas)
            {
                currentGas = gas;
                ApplyVisual();
                Turning?.Invoke(gas);
            }
        }

        private float? HandAngle()
        {
            var attach = firstInteractorSelecting?.GetAttachTransform(this);
            if (attach == null)
            {
                return null;
            }

            var axis = transform.up;
            var planar = Vector3.ProjectOnPlane(attach.position - transform.position, axis);
            if (planar.magnitude < deadZoneMeters)
            {
                return null;
            }

            return Vector3.SignedAngle(transform.forward, planar, axis);
        }

        // La pieza visible salta de un nivel de gas al siguiente, como una perilla con muescas.
        private void ApplyVisual()
        {
            if (knobVisual != null)
            {
                knobVisual.localRotation = Quaternion.Euler(0f, mapping.AngleForGas(currentGas), 0f);
            }
        }
    }
}
