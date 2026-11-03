using System.Collections;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Luz indicadora de la estación (encendido, aviso de quemado...). Se maneja desde los eventos del Inspector.
    // Todas las referencias son opcionales: sin ellas no hace nada.
    public sealed class RoastStationIndicatorLight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";

        [Header("Targets (all optional)")]
        [SerializeField] private Light targetLight;
        [SerializeField] private Renderer targetRenderer;

        [Header("Colors")]
        [SerializeField] private Color onColor = Color.green;
        [SerializeField] private Color offColor = Color.black;

        [Header("Emission (Renderer only)")]
        [SerializeField] private bool useEmission = true;
        [SerializeField] private Color onEmissionColor = Color.green;
        [SerializeField] private Color offEmissionColor = Color.black;

        [Header("Light")]
        [SerializeField] private bool lightOffDisablesLight = true;

        [Header("Behaviour")]
        [SerializeField] private bool startsOn;
        [Min(0.05f)]
        [SerializeField] private float blinkIntervalSeconds = 0.4f;

        private bool isOn;
        private Coroutine blinkRoutine;
        private Material materialInstance;

        public bool IsOn => isOn;
        public bool IsBlinking => blinkRoutine != null;

        public void SetOn()
        {
            isOn = true;
            if (!IsBlinking)
            {
                Apply(true);
            }
        }

        public void SetOff()
        {
            isOn = false;
            if (!IsBlinking)
            {
                Apply(false);
            }
        }

        // Parpadea entre encendido y apagado. Al parar vuelve al último SetOn/SetOff.
        public void StartBlink()
        {
            if (IsBlinking || !isActiveAndEnabled)
            {
                return;
            }

            blinkRoutine = StartCoroutine(Blink());
        }

        public void StopBlink()
        {
            if (blinkRoutine != null)
            {
                StopCoroutine(blinkRoutine);
                blinkRoutine = null;
            }

            Apply(isOn);
        }

        private void OnEnable()
        {
            isOn = startsOn;
            Apply(isOn);
        }

        private void OnDisable()
        {
            // Unity ya detiene las corrutinas al desactivar; aquí solo se limpia el estado.
            blinkRoutine = null;
        }

        private void OnDestroy()
        {
            if (materialInstance != null)
            {
                Destroy(materialInstance);
                materialInstance = null;
            }
        }

        private IEnumerator Blink()
        {
            var lit = true;
            while (true)
            {
                Apply(lit);
                lit = !lit;
                yield return new WaitForSeconds(blinkIntervalSeconds);
            }
        }

        private void Apply(bool on)
        {
            if (targetLight != null)
            {
                targetLight.color = on ? onColor : offColor;
                targetLight.enabled = on || !lightOffDisablesLight;
            }

            if (targetRenderer != null)
            {
                ApplyToRenderer(on);
            }
        }

        private void ApplyToRenderer(bool on)
        {
            // Se usa una copia del material para no modificar el asset compartido.
            if (materialInstance == null)
            {
                materialInstance = targetRenderer.material;
            }

            if (materialInstance == null)
            {
                return;
            }

            var color = on ? onColor : offColor;
            if (materialInstance.HasProperty(BaseColorId))
            {
                materialInstance.SetColor(BaseColorId, color);
            }

            if (materialInstance.HasProperty(ColorId))
            {
                materialInstance.SetColor(ColorId, color);
            }

            if (useEmission)
            {
                materialInstance.EnableKeyword(EmissionKeyword);
                if (materialInstance.HasProperty(EmissionColorId))
                {
                    materialInstance.SetColor(EmissionColorId, on ? onEmissionColor : offEmissionColor);
                }
            }
        }
    }
}
