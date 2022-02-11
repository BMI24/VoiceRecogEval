using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using Newtonsoft.Json;
using Plugin.AudioRecorder;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VoiceRecogEvalClient.Networking;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace VoiceRecogEvalClient
{
    public partial class MainPage : ContentPage
    {
        const string ServerIpString = "localhost";
        const int SampleRate = 44100;
        const int BytesPerSample = 2;
        const float SilenceTimeoutSeconds = 3;
        const float MinValidVoiceSeconds = 0.1f;
        readonly static Color ErrorColor = (Color)Application.Current.Resources["ErrorColor"];
        readonly static Color ProcessingColor = (Color)Application.Current.Resources["ProcessingColor"];
        readonly static Color UserInputColor = (Color)Application.Current.Resources["UserInputColor"];
        readonly static Color NeutralColor = (Color)Application.Current.Resources["NeutralColor"];
        IServerClient ServerClient;

        string Username;
        void SetBackgroundColor(Color color) => Device.BeginInvokeOnMainThread(() => ContentPage.ColorTo(ContentPage.BackgroundColor, color, c => ContentPage.BackgroundColor = c));
        void SetCenterText(string text) => Device.BeginInvokeOnMainThread(() => CenterText.Text = text);

        void SetBackgroundAndCenterText(Color color, string text)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ContentPage.ColorTo(ContentPage.BackgroundColor, color, c => ContentPage.BackgroundColor = c);
                CenterText.Text = TargetVoiceCount == 0 ? text : $"{CurrentVoiceCount}/{TargetVoiceCount}{Environment.NewLine}{Environment.NewLine}" + text;
            });
        }

        AudioRecorderService AudioRecorder;
        public MainPage(string username)
        {
            InitializeComponent();
            Username = username;
            ServerClient = new RestServerClient(ServerIpString, username);
            SetBackgroundColor(ErrorColor);

            AudioRecorder = new AudioRecorderService
            {
                PreferredSampleRate = SampleRate,
                StopRecordingAfterTimeout = false,
                StopRecordingOnSilence = false,
                AudioSilenceTimeout = TimeSpan.FromSeconds(SilenceTimeoutSeconds)
            };
            //cant set this. just hope for the best?
            //AudioRecorder.AudioStreamDetails.BitsPerSample = BytesPerSample * 8;
            AudioRecorder.AudioLevelUpdated += AudioChartView.NewAudioLevelReceived;

            CurrentPauseCancellationSource = new CancellationTokenSource();

            SetCenterText("Laden...");

            DisplayAlert("Hinweis", "Diese App nimmt den Ton über das Mikrofon auf und lädt diesen zur späteren Datenverarbeitung auf einen Server hoch. Die hochgeladenen Audiodateien beinhalten alle Töne, nicht nur gesprochene Sprache.", "Ok")
                .ContinueWith(t => GetConfigurationFromServer(CurrentPauseCancellationSource.Token));
        }

        int TargetVoiceCount;
        int CurrentVoiceCount;
        async Task GetConfigurationFromServer(CancellationToken cancellationToken)
        {
            ReturnToTask = t => GetConfigurationFromServer(t);

            TargetVoiceCount = await ServerClient.GetVoiceLineCountTarget(cancellationToken) ?? throw CreateFaultedServerCommException(cancellationToken);
            CurrentVoiceCount = await ServerClient.GetVoiceLineCountCurrent(cancellationToken) ?? throw CreateFaultedServerCommException(cancellationToken);

            _ = CalibrateMicrophoneLevel(CurrentPauseCancellationSource.Token);
        }

        async Task CalibrateMicrophoneLevel(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            ReturnToTask = t => CalibrateMicrophoneLevel(t);

            const int waitTime = 5;
            const int speakTime = 5;

            for (int i = 0; i < waitTime; i++)
            {
                SetBackgroundAndCenterText(NeutralColor, $"Bitte in {waitTime - i} Sekunden nichts sagen.");
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), cancellationToken.WaitHandle.WaitOneAsync());
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
            List<float> levels = new List<float>();
            void logToLevels(object sender, float e)
            {
                levels.Add(e);
            }
            AudioRecorder.AudioLevelUpdated += logToLevels;
            var startAudioTask = AudioRecorder.StartRecording();
            var recordAudioTask = await startAudioTask;

            SetBackgroundAndCenterText(UserInputColor, $"Bitte für ungefähr 10 Sekunden nichts sagen.");
            for (int i = 0; i < speakTime; i++)
            {
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), cancellationToken.WaitHandle.WaitOneAsync());
                if (cancellationToken.IsCancellationRequested)
                {
                    await AudioRecorder.StopRecording();
                    return;
                }
            }

            await AudioRecorder.StopRecording();
            AudioRecorder.AudioLevelUpdated -= logToLevels;
            var averageLevel = levels.Average();
            var stdeviationLevel = (float)Math.Sqrt(levels.Average(v => Math.Pow(v - averageLevel, 2)));
            levels.Sort();
            var silenceThreshold = levels[(int)(levels.Count * 0.95f)] + 2 * stdeviationLevel;

            for (int i = 0; i < waitTime; i++)
            {
                SetBackgroundAndCenterText(NeutralColor, $"Bitte in {waitTime - i} Sekunden von 1 aus in normaler Lautstärke aufwärts zählen.");
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), cancellationToken.WaitHandle.WaitOneAsync());
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            int totalCount = 0;
            int nonSilenceCount = 0;
            void countNonSilence(object sender, float e)
            {
                totalCount++;
                if (e > silenceThreshold)
                    nonSilenceCount++;
            }

            AudioRecorder.AudioLevelUpdated += countNonSilence;
            startAudioTask = AudioRecorder.StartRecording();
            recordAudioTask = await startAudioTask;

            SetBackgroundAndCenterText(UserInputColor, $"Bitte für ungefähr 10 Sekunden von 1 aus in normaler Lautstärke aufwärts zählen.");
            for (int i = 0; i < speakTime; i++)
            {
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), cancellationToken.WaitHandle.WaitOneAsync());
                if (cancellationToken.IsCancellationRequested)
                {
                    await AudioRecorder.StopRecording();
                    return;
                }
            }

            await AudioRecorder.StopRecording();
            AudioRecorder.AudioLevelUpdated -= countNonSilence;

            float nonSilenceFraction = (float)nonSilenceCount / totalCount;

            if (nonSilenceFraction < 0.6f)
            {
                SetBackgroundColor(ErrorColor);

                _ = Task.Run(async () =>
                {
                    const int secondsToDisplay = 10;
                    for (int i = 0; i < secondsToDisplay; i++)
                    {
                        SetCenterText("Fehler: Stimme kann nicht gut erkannt werden. Bitte ruhigere Umgebung aufsuchen. Neuversuch in " + (secondsToDisplay - i) + " Sekunden.");
                        await Task.Delay(TimeSpan.FromSeconds(1));
                        if (cancellationToken.IsCancellationRequested) return;
                    }
                    ReturnToLastTask(cancellationToken);
                });
                return;
            }

            AudioRecorder.SilenceThreshold = silenceThreshold;
            AudioRecorder.StopRecordingOnSilence = true;
            AudioChartView.SilenceThreshold = silenceThreshold;
            AudioChartView.IsUpdating = true;

            _ = GetDesiredVoiceLineFromServer(cancellationToken);
        }

        async Task GetDesiredVoiceLineFromServer(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            ReturnToTask = t => GetDesiredVoiceLineFromServer(t);

            SetBackgroundAndCenterText(ProcessingColor, "Warte auf Server - erwartete Phrase");


            var requestedAudioString = await ServerClient.GetRequestedPhrase(cancellationToken) ?? throw CreateFaultedServerCommException(cancellationToken);

            _ = GetUserRecordedAudio(requestedAudioString, cancellationToken);
        }

        async Task GetUserRecordedAudio(PhraseRequestData requestedAudioInfo, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            ReturnToTask = t => GetUserRecordedAudio(requestedAudioInfo, t);

            SetBackgroundAndCenterText(UserInputColor, "Bitte einmal folgende Phrase sprechen: " + '"' + requestedAudioInfo.Phrase + '"' + " und dann 3 Sekunden still sein.");

            if (cancellationToken.IsCancellationRequested) return;
            
            var startAudioTask = AudioRecorder.StartRecording();
            var recordAudioTask = await startAudioTask;

            await Task.WhenAny(recordAudioTask, cancellationToken.WaitHandle.WaitOneAsync());
            if (cancellationToken.IsCancellationRequested)
            {
                _ = AudioRecorder.StopRecording();
                return;
            }


            var audioPath = AudioRecorder.FilePath;

            var audioSize = new FileInfo(AudioRecorder.FilePath).Length;
            float wavDuration = audioSize / (SampleRate * BytesPerSample);
            if (cancellationToken.IsCancellationRequested) return;
            if (wavDuration < SilenceTimeoutSeconds + MinValidVoiceSeconds)
            {
                SetBackgroundColor(ErrorColor);
                for (int i = 0; i < 3; i++)
                {
                    SetCenterText("Fehler: nichts verstanden. Bitte in " + (3 - i) + " Sekunden wiederholen");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (cancellationToken.IsCancellationRequested) return;
                }
                _ = GetUserRecordedAudio(requestedAudioInfo, cancellationToken);
            }
            else
                _ = UploadFile(requestedAudioInfo, audioPath, cancellationToken);
        }
        async Task UploadFile(PhraseRequestData requestedAudioInfo, string audioPath, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            ReturnToTask = t => UploadFile(requestedAudioInfo, audioPath, t);

            SetBackgroundAndCenterText(ProcessingColor, "Lade Daten auf Server hoch");

            var uploadSuccess = await ServerClient.UploadPhrase(requestedAudioInfo, audioPath, cancellationToken);
            if (!uploadSuccess)
                throw CreateFaultedServerCommException(cancellationToken);

            CurrentVoiceCount++;
            
            _ = GetDesiredVoiceLineFromServer(cancellationToken);
        }

        Exception CreateFaultedServerCommException(CancellationToken cancellationToken)
        {
            SetBackgroundColor(ErrorColor);

            Task.Run(async () =>
            {
                const int secondsToWait = 3;
                for (int i = 0; i < secondsToWait; i++)
                {
                    SetCenterText("Serverantwort: Fehler. Neuversuch in " + (secondsToWait - i) + " Sekunden.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    if (cancellationToken.IsCancellationRequested) return;
                }
                ReturnToLastTask(cancellationToken);
            });
            return new Exception();
        }

        private void ReturnToLastTask(CancellationToken token)
        {
            _ = ReturnToTask(CurrentPauseCancellationSource.Token);
        }

        bool Paused;
        Func<CancellationToken, Task> ReturnToTask;
        CancellationTokenSource CurrentPauseCancellationSource;

        private void PauseResumeButton_Clicked(object sender, EventArgs e)
        {
            if (Paused)
            {
                CurrentPauseCancellationSource = new CancellationTokenSource();
                ReturnToLastTask(CurrentPauseCancellationSource.Token);

                Device.BeginInvokeOnMainThread(() => PauseResumeButton.Text = "Pause");
                Paused = false;
            }
            else
            {
                SetBackgroundAndCenterText(NeutralColor, "Pausiert");
                CurrentPauseCancellationSource.Cancel();

                Device.BeginInvokeOnMainThread(() => PauseResumeButton.Text = "Weiter");
                Paused = true;
            }
        }
    }
}
