using Android.Content;
using Android.App;
using Android.Speech.Tts;
using KidsDictionaryApp.Services.Interfaces;
using TextToSpeech = Android.Speech.Tts.TextToSpeech;
using Application = Android.App.Application;

namespace KidsDictionaryApp.Platforms.Android
{
    public class AndroidTextToSpeechService : Java.Lang.Object, ITextToSpeechService, TextToSpeech.IOnInitListener
    {
        private TextToSpeech _tts;

        public AndroidTextToSpeechService()
        {
            _tts = new TextToSpeech(Application.Context, this);
        }

        public void OnInit(OperationResult status)
        {
            if (status == OperationResult.Success)
            {
                _tts.SetLanguage(Java.Util.Locale.Us);
            }
        }

        public Task SpeakAsync(string text)
        {
            _tts.Speak(text, QueueMode.Flush, null, null);
            return Task.CompletedTask;
        }
    }
}