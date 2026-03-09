using Android.Content;
using Android.OS;
using Android.Speech;
using KidsDictionaryApp.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace KidsDictionaryApp.Platforms.Android
{
    public class AndroidSpeechService : ISpeechService
    {
        private readonly ILogger<AndroidSpeechService> _logger;

        public AndroidSpeechService(ILogger<AndroidSpeechService> logger)
        {
            _logger = logger;
        }

        public async Task<string?> ListenAsync()
        {
            // Request RECORD_AUDIO permission at runtime (required on Android 6.0+).
            var permissionStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                _logger.LogWarning("Android speech recognition: microphone permission not granted.");
                return null;
            }

            var tcs = new TaskCompletionSource<string?>();

            // SpeechRecognizer must be created and used on the UI thread (main Looper).
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                try
                {
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (activity == null)
                    {
                        _logger.LogWarning("Android speech recognition: no current activity available.");
                        tcs.TrySetResult(null);
                        return;
                    }

                    if (!SpeechRecognizer.IsRecognitionAvailable(activity))
                    {
                        _logger.LogWarning("Android speech recognition is not available on this device.");
                        tcs.TrySetResult(null);
                        return;
                    }

                    var recognizer = SpeechRecognizer.CreateSpeechRecognizer(activity);
                    if (recognizer == null)
                    {
                        _logger.LogWarning("Android speech recognizer could not be created.");
                        tcs.TrySetResult(null);
                        return;
                    }

                    try
                    {
                        var listener = new SpeechListener(tcs, () => recognizer.Destroy(), _logger);
                        recognizer.SetRecognitionListener(listener);

                        var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                        intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                        intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                        intent.PutExtra(RecognizerIntent.ExtraCallingPackage, activity.PackageName);

                        recognizer.StartListening(intent);
                    }
                    catch
                    {
                        recognizer.Destroy();
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Android speech recognition setup failed.");
                    tcs.TrySetException(ex);
                }
            });

            return await tcs.Task;
        }

        private sealed class SpeechListener : Java.Lang.Object, IRecognitionListener
        {
            private readonly TaskCompletionSource<string?> _tcs;
            private readonly Action _cleanup;
            private readonly ILogger _logger;

            public SpeechListener(TaskCompletionSource<string?> tcs, Action cleanup, ILogger logger)
            {
                _tcs = tcs;
                _cleanup = cleanup;
                _logger = logger;
            }

            public void OnResults(Bundle? results)
            {
                var matches = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
                _tcs.TrySetResult(matches?.FirstOrDefault());
                _cleanup();
            }

            public void OnError(SpeechRecognizerError error)
            {
                _logger.LogWarning("Android speech recognition error: {Error}", error);
                _tcs.TrySetResult(null);
                _cleanup();
            }

            public void OnReadyForSpeech(Bundle? @params) { }
            public void OnBeginningOfSpeech() { }
            public void OnRmsChanged(float rmsdB) { }
            public void OnBufferReceived(byte[]? buffer) { }
            public void OnEndOfSpeech() { }
            public void OnPartialResults(Bundle? partialResults) { }
            public void OnEvent(int eventType, Bundle? @params) { }
        }
    }
}