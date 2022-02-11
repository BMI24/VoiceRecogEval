using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.Models
{
    public class VoiceRecognitionResult
    {
        public string Result;
        public List<VoiceRecognitionResultPart> Parts;
        public VoiceRecognitionResult(string result, List<VoiceRecognitionResultPart> parts)
        {
            Result = result;
            Parts = parts;
        }
    }
}
