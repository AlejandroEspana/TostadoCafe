using System;
using UnityEngine;

namespace ToastedVr.RoastVoice
{
    public static class VoiceClipFactory
    {
        // Devuelve null si no hay audio o no se puede leer: en ese caso el jugador ve solo el texto.
        public static AudioClip TryCreate(VoiceAudio audio, string clipName)
        {
            if (audio == null || string.IsNullOrWhiteSpace(audio.Base64))
            {
                return null;
            }

            try
            {
                var wav = WavCodec.DecodePcm16(Convert.FromBase64String(audio.Base64));
                var frames = wav.Samples.Length / wav.Channels;
                if (frames == 0)
                {
                    return null;
                }

                var clip = AudioClip.Create(clipName, frames, wav.Channels, wav.SampleRateHz, false);
                clip.SetData(wav.Samples, 0);
                return clip;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
