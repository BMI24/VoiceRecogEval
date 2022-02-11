using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.Models
{
    public class VoiceRecognitionResultPart
    {
        public float Confidence;
        public float End;
        public float Start;
        public string Word;

        public VoiceRecognitionResultPart(string word, float start, float end, float confidence)
        {
            Confidence = confidence;
            End = end;
            Start = start;
            Word = word;
        }
    }
}
