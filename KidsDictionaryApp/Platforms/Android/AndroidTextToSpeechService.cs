using Android.Speech.Tts;
using KidsDictionaryApp.Services.Interfaces;
using Microsoft.Extensions.Logging;
using TextToSpeech = Android.Speech.Tts.TextToSpeech;
using Application = Android.App.Application;

namespace KidsDictionaryApp.Platforms.Android
{
    public class AndroidTextToSpeechService : Java.Lang.Object, ITextToSpeechService, TextToSpeech.IOnInitListener
    {
        private readonly TextToSpeech _tts;
        private readonly TaskCompletionSource<bool> _initTcs = new();
        private readonly ILogger<AndroidTextToSpeechService> _logger;

        public AndroidTextToSpeechService(ILogger<AndroidTextToSpeechService> logger)
        {
            _logger = logger;
            _tts = new TextToSpeech(Application.Context, this);
        }

        public void OnInit(OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                _tts.SetLanguage(Java.Util.Locale.Us);
                _initTcs.TrySetResult(true);
            }
            else
            {
                _logger.LogError("Android TTS engine failed to initialize with status: {Status}", status);
                _initTcs.TrySetResult(false);
            }
        }

        public async Task SpeakAsync(string text)
        {
            if (!await _initTcs.Task)
            {
                _logger.LogWarning("Android TTS is not available; skipping speak.");
                return;
            }

            var utteranceId = Guid.NewGuid().ToString();
            var speakTcs = new TaskCompletionSource<bool>();
            _tts.SetOnUtteranceProgressListener(new UtteranceListener(speakTcs));
            _tts.Speak(text, QueueMode.Flush, null, utteranceId);
            await speakTcs.Task;
        }

        private sealed class UtteranceListener : UtteranceProgressListener
        {
            private readonly TaskCompletionSource<bool> _tcs;

            public UtteranceListener(TaskCompletionSource<bool> tcs)
            {
                _tcs = tcs;
            }

            public override void OnDone(string? utteranceId) => _tcs.TrySetResult(true);
            public override void OnError(string? utteranceId) => _tcs.TrySetResult(false);
            public override void OnStart(string? utteranceId) { }
        }
    }
}