using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class InvalidAction : VoiceAction
    {
        public override string ToString()
        {
            return "invalid";
        }

        public override bool Equals(object obj)
        {
            return obj is InvalidAction;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
