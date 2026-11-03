using System.Collections.Generic;
using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Cambia el color de los granos según el grainState del tueste.
    // Se conecta al evento "Grain State Changed (String)" de RoastStationVisualEvents.
    public sealed class RoastGrainVisualController : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private Renderer[] targetRenderers;

        [Header("Colors")]
        [SerializeField] private Color rawGreen = new Color(0.45f, 0.55f, 0.28f);
        [SerializeField] private Color yellow = new Color(0.80f, 0.70f, 0.30f);
        [SerializeField] private Color lightBrown = new Color(0.62f, 0.42f, 0.22f);
        [SerializeField] private Color mediumBrown = new Color(0.40f, 0.25f, 0.13f);
        [SerializeField] private Color darkBrown = new Color(0.20f, 0.11f, 0.06f);
        [SerializeField] private Color burnedBlack = new Color(0.04f, 0.03f, 0.03f);

        // Copias propias de los materiales: así nunca se tocan los assets compartidos.
        private readonly List<Material> materialInstances = new List<Material>();

        private void Awake()
        {
            if (targetRenderers == null)
            {
                return;
            }

            foreach (var targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                // renderer.materials devuelve instancias nuevas y las asigna al renderer.
                materialInstances.AddRange(targetRenderer.materials);
            }
        }

        // Compatible con la opción Dynamic String del UnityEvent.
        public void SetGrainState(string grainState)
        {
            if (!TryGetColor(grainState, out var color))
            {
                return; // Estado desconocido: se deja el color actual.
            }

            foreach (var material in materialInstances)
            {
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, color);
                }

                if (material.HasProperty(ColorId))
                {
                    material.SetColor(ColorId, color);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var material in materialInstances)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }

            materialInstances.Clear();
        }

        // Estados del backend: DRYING, MAILLARD, FIRST_CRACK, DARK, SECOND_CRACK y BURNED.
        private bool TryGetColor(string grainState, out Color color)
        {
            color = default;
            if (string.IsNullOrWhiteSpace(grainState))
            {
                return false;
            }

            switch (grainState.Trim().ToUpperInvariant())
            {
                case "DRYING": color = rawGreen; return true;
                case "MAILLARD": color = yellow; return true;
                case "FIRST_CRACK": color = lightBrown; return true;
                case "DARK": color = mediumBrown; return true;
                case "SECOND_CRACK": color = darkBrown; return true;
                case "BURNED": color = burnedBlack; return true;
                default: return false;
            }
        }
    }
}
