using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Update;
using VoiceRecogEvalServer.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace VoiceRecogEvalServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecordingUploadController : ControllerBase
    {
        IPhraseRecognizer PhraseRecognizer;
        IPhraseRequestSelector PhraseRequestSelctor;
        public RecordingUploadController(IPhraseRecognizer recognizer, IPhraseRequestSelector requestSelector)
        {
            PhraseRecognizer = recognizer;
            PhraseRequestSelctor = requestSelector;
        }
        public class VoiceUploadModel
        {
            public string Username { get; set; }
            public string RequestedVoiceLine { get; set; }
        }
        public class EvaluationResult
        {
            public struct EvaluationResultPart
            {
                public EvaluationResultPart(string phrase, int count, double accuracy) : this()
                {
                    Phrase = phrase;
                    Count = count;
                    Accuracy = accuracy;
                }

                public string Phrase { get; set; }
                public int Count { get; set; }
                public double Accuracy { get; set; }
            }
            public EvaluationResult(string message)
            {
                Message = message;
            }

            public List<EvaluationResultPart> AccuracyData { get; set; } = new List<EvaluationResultPart>();
            public string Message { get; set; }
        }

        static Dictionary<int, Task<EvaluationResult>> ResultTasks = new Dictionary<int, Task<EvaluationResult>>();

        private async Task<EvaluationResult> GenerateResult()
        {
            var result = new EvaluationResult("Evaluation completed");
            var voiceLineAccuracy = new Dictionary<string, (int count, int correct)>();
            var wrongRecordingIds = new List<int>();
            using (var db = new RecordingDatabaseContext())
            {
                foreach (var recording in db.Recordings)
                {
                    if (!PhraseRequestSelctor.IsRequestedPhrase(recording.RequestedVoiceLine) || recording.HasUserError)
                        continue;

                    var isSingleWord = !recording.RequestedVoiceLine.Contains(' ');
                    var recognitionResult = await PhraseRecognizer.Process(new MemoryStream(recording.SoundFile, false));
                    bool correct = recording.RequestedVoiceLine == recognitionResult;
                    Console.WriteLine($"Wanted: \"{recording.RequestedVoiceLine}\", got: \"{recognitionResult}\"");
                    if (!voiceLineAccuracy.ContainsKey(recording.RequestedVoiceLine))
                        voiceLineAccuracy[recording.RequestedVoiceLine] = (0, 0);

                    voiceLineAccuracy[recording.RequestedVoiceLine] = 
                        (voiceLineAccuracy[recording.RequestedVoiceLine].count + 1,
                        voiceLineAccuracy[recording.RequestedVoiceLine].correct + (correct ? 1 : 0));

                    if (!correct)
                        wrongRecordingIds.Add(recording.RecordingId);
                }
            }

            Console.WriteLine("Completed evaluation. Ids of incorrect recordings: " + string.Join(", ", wrongRecordingIds));

            result.AccuracyData = voiceLineAccuracy
                .Select(kv => new EvaluationResult.EvaluationResultPart(kv.Key, kv.Value.count, kv.Value.correct / (double)kv.Value.count))
                .OrderBy(r => r.Accuracy)
                .ToList();

            return result;
        }

        [HttpGet("req")]
        public string Get()
        {
            return PhraseRequestSelctor.GetNextRequestedPhrase();
        }

        [HttpGet("count/{username}")]
        public int GetCount(string username)
        {
            using (var db = new RecordingDatabaseContext())
            {
                return db.Recordings.Count(r => r.Username.ToLower() == username.ToLower() && !r.HasUserError && PhraseRequestSelctor.IsRequestedPhrase(r.RequestedVoiceLine));
            }
        }


        // GET: api/<RecordingUploadController>
        [HttpGet("res/{id}")]
        public EvaluationResult Get(int id)
        {
            if (ResultTasks.TryGetValue(id, out var resultTask))
            {
                if (resultTask.IsCompleted)
                {
                    return resultTask.Result;
                }
                else
                {
                    return new EvaluationResult("Still loading");
                }
            }
            else
            {
                ResultTasks.Add(id, Task.Run(GenerateResult));
                return new EvaluationResult("Initialized loading");
            }
        }

        static bool IsWavFile(byte[] file)
        {
            // see https://en.wikipedia.org/wiki/List_of_file_signatures
            return file[0] == 0x52 &&
                file[1] == 0x49 &&
                file[2] == 0x46 &&
                file[3] == 0x46 &&
                // file[4] == ignore &&
                // file[5] == ignore &&
                // file[6] == ignore &&
                // file[7] == ignore &&
                file[8] == 0x57 &&
                file[9] == 0x41 &&
                file[10] == 0x56 &&
                file[11] == 0x45;
        }

        // POST api/<RecordingUploadController>
        [HttpPost]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> Post()
        {
            // const int kilobyteMultiplier = 1024;
            // const int megabyteMultiplier = 1024 * kilobyteMultiplier;
            // const int maxFileSize = 10 * megabyteMultiplier;
            // with 44100khz*16bit= 705600b/s = 88200 B/s waveform: 30s ~ 2.64 MB

            var recording = new VoiceRecording();

            FormValueProvider formModel;
            using (var stream = new MemoryStream())
            {
                formModel = await Request.StreamFile(stream);
                recording.SoundFile = stream.ToArray();
            }

            var input = new VoiceUploadModel();

            if (!await TryUpdateModelAsync(input, prefix: "", valueProvider: formModel))
                return BadRequest("Format does not match");

            if (recording.SoundFile == null)
                return BadRequest(nameof(recording.SoundFile) + " must be supplied");
            if (!IsWavFile(recording.SoundFile))
                return BadRequest(nameof(recording.SoundFile) + " must be in wav format");
            if (input.Username == null)
                return BadRequest(nameof(input.Username) + " must be supplied");
            if (input.RequestedVoiceLine == null)
                return BadRequest(nameof(input.RequestedVoiceLine) + " must be supplied");

            //if (input.File.Length > maxFileSize)
            //    return BadRequest("Embedded file size may not be more then " + maxFileSize + " bytes");

            recording.Username = input.Username;
            recording.RequestedVoiceLine = input.RequestedVoiceLine;
            recording.UploadedUtcTime = DateTime.UtcNow;

            using (var db = new RecordingDatabaseContext())
            {
                await db.AddAsync(recording);
                await db.SaveChangesAsync();
            }

            return Ok(recording.RecordingId);
        }
    }
}
