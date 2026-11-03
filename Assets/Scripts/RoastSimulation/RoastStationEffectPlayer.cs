using UnityEngine;

namespace ToastedVr.RoastSimulation
{
    // Dispara un efecto (partículas, sonido, objeto visual) desde los eventos del Inspector.
    // Todas las referencias son opcionales: sin ellas no hace nada.
    public sealed class RoastStationEffectPlayer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Optional object that is activated on Play and deactivated on Stop.")]
        [SerializeField] private GameObject visual;

        public void Play()
        {
            if (visual != null)
            {
                visual.SetActive(true);
            }

            if (particles != null)
            {
                particles.Play();
            }

            if (audioSource != null && audioSource.isActiveAndEnabled)
            {
                audioSource.Play();
            }
        }

        public void Stop()
        {
            if (particles != null)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            if (visual != null)
            {
                visual.SetActive(false);
            }
        }
    }
}
