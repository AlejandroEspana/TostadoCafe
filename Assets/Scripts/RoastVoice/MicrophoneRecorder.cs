using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace ToastedVr.RoastVoice
{
    public enum MicrophoneStartResult
    {
        Started,
        NoDevice,
        PermissionPending
    }

    // Envuelve el Microphone de Unity: graba en un clip que no da la vuelta y entrega lo grabado en mono a 16 kHz.
    public sealed class MicrophoneRecorder
    {
        private readonly int targetRateHz;
        private readonly int maxSeconds;
        private string device;
        private AudioClip clip;
        private int recordingRateHz;

        public MicrophoneRecorder(int targetRateHz, float maxSeconds)
        {
            this.targetRateHz = targetRateHz;
            this.maxSeconds = Mathf.CeilToInt(maxSeconds);
        }

        public bool IsRecording => clip != null && Microphone.IsRecording(device);

        public float RecordedSeconds => clip != null && recordingRateHz > 0 ? (float)Microphone.GetPosition(device) / recordingRateHz : 0f;

        // En el visor el permiso se pide la primera vez; mientras el jugador no responde, la grabación no empieza.
        public MicrophoneStartResult Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                return MicrophoneStartResult.PermissionPending;
            }
#endif
            if (Microphone.devices.Length == 0)
            {
                return MicrophoneStartResult.NoDevice;
            }

            device = Microphone.devices[0];
            recordingRateHz = ChooseRate(device);
            clip = Microphone.Start(device, false, maxSeconds, recordingRateHz);
            return clip != null ? MicrophoneStartResult.Started : MicrophoneStartResult.NoDevice;
        }

        public float[] Stop()
        {
            if (clip == null)
            {
                return new float[0];
            }

            var recordedFrames = Microphone.GetPosition(device);
            Microphone.End(device);

            // Si se cortó solo en el máximo, la posición vuelve a 0 y vale el clip completo.
            var frames = recordedFrames > 0 ? recordedFrames : clip.samples;
            var interleaved = new float[frames * clip.channels];
            clip.GetData(interleaved, 0);
            var channels = clip.channels;
            Object.Destroy(clip);
            clip = null;

            return PcmResampler.Resample(PcmResampler.ToMono(interleaved, channels), recordingRateHz, targetRateHz);
        }

        // Pide 16 kHz; si el dispositivo no lo permite, usa el valor más cercano dentro de su rango y luego se convierte.
        private int ChooseRate(string deviceName)
        {
            Microphone.GetDeviceCaps(deviceName, out var minRate, out var maxRate);
            if (minRate == 0 && maxRate == 0)
            {
                return targetRateHz;
            }

            return Mathf.Clamp(targetRateHz, minRate, maxRate);
        }
    }
}
