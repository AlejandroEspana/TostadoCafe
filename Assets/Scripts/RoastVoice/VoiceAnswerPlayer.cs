using UnityEngine;

namespace ToastedVr.RoastVoice
{
    // Reproduce desde la estación, en 3D, la voz del asistente.
    [RequireComponent(typeof(AudioSource))]
    public sealed class VoiceAnswerPlayer : MonoBehaviour
    {
        private AudioSource source;

        public bool IsPlaying => source != null && source.isPlaying;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Play(AudioClip clip)
        {
            Stop();
            source.clip = clip;
            source.Play();
        }

        public void Stop()
        {
            if (source.clip == null)
            {
                return;
            }

            source.Stop();
            var previous = source.clip;
            source.clip = null;
            Destroy(previous);
        }

        private void OnDestroy()
        {
            if (source != null && source.clip != null)
            {
                Destroy(source.clip);
            }
        }
    }
}
