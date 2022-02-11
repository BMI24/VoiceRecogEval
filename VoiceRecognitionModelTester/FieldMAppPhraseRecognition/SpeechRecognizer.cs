using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kaldi;
using Newtonsoft.Json;
using VoiceRecogEvalServer.Models;
using static VoiceRecogEvalServer.FieldMAppPhraseRecognition.VoiceCommandCompiler;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class SpeechRecognizer
    {
        class PartialResult
        {
            public string partial;
        }

        class Result
        {
            public string text;
            public List<ResultPart> result;
        }

        class ResultPart
        {
            public float conf;
            public float end;
            public float start;
            public string word;
        }

        const string VoskModelPathName = "voskModelDe/";
        const int RecognizerSlots = 1;
        const int SampleRate = 44100;

        readonly string Lang = $"[{'"'}{string.Join(' ', AcceptedWords.Where(k => k != "[unk]"))}{'"'}, {'"'}[unk]{'"'}]";

        public static readonly List<string> AcceptedWords = KeywordStringToSymbol
            .Select(kv => kv.Key)
            .ToList();


        KaldiRecognizer KaldiRecognizer;
        SemaphoreSlim KaldiRecognizerLock = new SemaphoreSlim(RecognizerSlots);

        public Task LoadTask { get; private set; }

        public bool IsLoaded => LoadTask.IsCompletedSuccessfully;
        public bool IsFaulted => LoadTask.IsFaulted;

        public Exception LoadException => IsFaulted ? LoadTask.Exception : null;

        public SpeechRecognizer()
        {
            LoadTask = Task.Run(Initialize);
        }

        public void Initialize()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                try
                {
                    Model = new Model(VoskModelPathName);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                LoadTask = Task.FromException(new Exception("Vosk only works in linux"));
            }
        }

        Model Model;

        public async Task<VoiceRecognitionResult> Process(Stream waveformStream)
        {
            if (LoadTask.IsFaulted)
                throw LoadTask.Exception;

            await KaldiRecognizerLock.WaitAsync();

            KaldiRecognizer = new KaldiRecognizer(Model, SampleRate, Lang);

            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = await waveformStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                KaldiRecognizer.AcceptWaveform(buffer, bytesRead);
            }

            var resultJson = KaldiRecognizer.FinalResult();

            KaldiRecognizerLock.Release();

            var result = JsonConvert.DeserializeObject<Result>(resultJson);

            return new VoiceRecognitionResult(result.text, result.result?.Select(r => new VoiceRecognitionResultPart(r.word, r.start, r.end, r.conf)).ToList());
        }
    }
}
