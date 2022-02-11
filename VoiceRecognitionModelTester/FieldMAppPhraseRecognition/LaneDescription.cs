using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public struct LaneDescription
    {
        public LaneDescription(int index)
        {
            Index = index;
        }
        public int Index { get; }
    }
}
