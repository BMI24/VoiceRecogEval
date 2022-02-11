using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class CancelAction : VoiceAction
    {
        public override string ToString()
        {
            return "cancel";
        }

        public override bool Equals(object obj)
        {
            return obj is CancelAction;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
