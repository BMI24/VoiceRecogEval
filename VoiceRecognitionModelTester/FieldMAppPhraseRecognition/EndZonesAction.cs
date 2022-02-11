using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class EndZonesAction : ZonesAction
    {
        public override string ToString()
        {
            return "end " + base.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is EndZonesAction other))
                return false;
            return other.Lanes.SetEquals(Lanes);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Lanes);
        }
    }
}
