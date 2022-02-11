using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public abstract class ZonesAction : VoiceAction
    {
        public HashSet<LaneDescription> Lanes = new HashSet<LaneDescription>();
        public override string ToString()
        {
            return '{' + string.Join(", ", Lanes.Select(i => i.Index.ToString())) + '}';
        }
    }
}
