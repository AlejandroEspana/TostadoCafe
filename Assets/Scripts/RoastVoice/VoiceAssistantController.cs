using System;
using System.Collections;
using ToastedVr.RoastSimulation;
using ToastedVr.SessionRefresh;
using UnityEngine;

namespace ToastedVr.RoastVoice
{
    public enum VoiceAssistantState
    {
        Idle,
        Recording,
        Waiting,
        Speaking
    }

    // Pregunta por voz de mantener presionado: graba mientras el jugador presiona y envía al soltar.
    public sealed class VoiceAssistantController : MonoBehaviour
    {
        private const string AnswerClipName = "AssistantAnswer";

        [SerializeField] private RoastVoiceConfiguration configuration;
        [SerializeField] private RoastVoiceMessages messages;
        [SerializeField] private RoastStationController station;
        [SerializeField] private VoiceAnswerPlayer player;

        private VoiceAssistantApiClient client;
        private MicrophoneRecorder recorder;
        private Coroutine recordingLimit;

        public event Action Changed;
        public event Action<string> SubtitleChanged;

        public RoastVoiceMessages Messages => messages;
        public VoiceAssistantState State { get; private set; }

        // Entradas recording y assistantBusy del evaluador de alertas proactivas.
        public bool IsRecording => State == VoiceAssistantState.Recording;
        public bool IsBusy => State == VoiceAssistantState.Waiting || State == VoiceAssistantState.Speaking;

        public bool CanAsk => client != null && client.IsConfigured && State == VoiceAssistantState.Idle
            && station.IsPowered && station.State != null;

        public void Initialize(string backendBaseUrl, AuthorizedRequestSender sender)
        {
            client = new VoiceAssistantApiClient(backendBaseUrl, configuration.QuestionsPath, configuration.RequestTimeoutSeconds, sender);
            recorder = new MicrophoneRecorder(configuration.SampleRateHz, configuration.MaxRecordingSeconds);
            station.Changed += NotifyChanged;
            NotifyChanged();
        }

        public void BeginQuestion()
        {
            if (!CanAsk)
            {
                return;
            }

            switch (recorder.Start())
            {
                case MicrophoneStartResult.NoDevice:
                    ShowSubtitle(messages.NoMicrophoneMessage);
                    return;
                case MicrophoneStartResult.PermissionPending:
                    ShowSubtitle(messages.MicrophonePermissionMessage);
                    return;
            }

            player.Stop();
            SetState(VoiceAssistantState.Recording);
            ShowSubtitle(messages.ListeningMessage);
            recordingLimit = StartCoroutine(StopAtMaximum());
        }

        public void EndQuestion()
        {
            if (State != VoiceAssistantState.Recording)
            {
                return;
            }

            if (recordingLimit != null)
            {
                StopCoroutine(recordingLimit);
                recordingLimit = null;
            }

            var samples = recorder.Stop();
            switch (RecordingRules.Check(samples.Length, configuration.SampleRateHz, configuration.MinRecordingSeconds, configuration.MaxAudioBytes))
            {
                case RecordingVerdict.TooShort:
                    ShowSubtitle(messages.TooShortMessage);
                    SetState(VoiceAssistantState.Idle);
                    return;
                case RecordingVerdict.TooLarge:
                    ShowSubtitle(messages.TooLargeMessage);
                    SetState(VoiceAssistantState.Idle);
                    return;
            }

            if (!station.IsPowered || station.State == null)
            {
                SetState(VoiceAssistantState.Idle);
                return;
            }

            var context = VoiceContextBuilder.Build(station.SimulationId, station.State);
            StartCoroutine(Send(WavCodec.EncodeMono16(samples, configuration.SampleRateHz), context));
        }

        private IEnumerator StopAtMaximum()
        {
            yield return new WaitForSecondsRealtime(configuration.MaxRecordingSeconds);
            recordingLimit = null;
            EndQuestion();
        }

        private IEnumerator Send(byte[] wav, VoiceQuestionContext context)
        {
            SetState(VoiceAssistantState.Waiting);
            ShowSubtitle(messages.ThinkingMessage);

            RoastApiResult<VoiceAnswer> result = null;
            yield return client.Ask(wav, context, r => result = r);

            if (!result.IsSuccess)
            {
                ShowFailure(result);
                SetState(VoiceAssistantState.Idle);
                yield break;
            }

            ShowSubtitle(result.Value.Answer);
            var clip = VoiceClipFactory.TryCreate(result.Value.Audio, AnswerClipName);
            if (clip == null)
            {
                SetState(VoiceAssistantState.Idle);
                yield break;
            }

            player.Play(clip);
            SetState(VoiceAssistantState.Speaking);
            while (player.IsPlaying)
            {
                yield return null;
            }

            SetState(VoiceAssistantState.Idle);
        }

        // Con 401 el refresh ya decidió: si lo rechazaron, SessionRefreshService avisa y vuelve al login.
        private void ShowFailure(RoastApiResult<VoiceAnswer> result)
        {
            if (result.Failure == RoastApiFailure.Unauthorized)
            {
                return;
            }

            if (result.Error?.Message != null)
            {
                ShowSubtitle(result.Error.Message);
                return;
            }

            ShowSubtitle(result.Failure == RoastApiFailure.Unavailable ? messages.OfflineMessage : messages.UnexpectedErrorMessage);
        }

        private void SetState(VoiceAssistantState state)
        {
            State = state;
            NotifyChanged();
        }

        private void ShowSubtitle(string text) => SubtitleChanged?.Invoke(text);

        private void NotifyChanged() => Changed?.Invoke();

        private void OnDestroy()
        {
            if (station != null)
            {
                station.Changed -= NotifyChanged;
            }

            if (State == VoiceAssistantState.Recording)
            {
                recorder.Stop();
            }
        }
    }
}
