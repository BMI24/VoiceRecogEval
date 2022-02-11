using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class SetZonesDetailAction : ZonesAction
    {
        public KeywordSymbol DamageCause = KeywordSymbol.invalid;
        public KeywordSymbol DamageType = KeywordSymbol.invalid;
        public bool ShouldEndZone;
        public override string ToString()
        {
            string result = base.ToString();
            if (DamageCause != KeywordSymbol.invalid)
                result += " cause: " + VoiceCommandCompiler.KeywordStringToSymbol.First(kv => kv.Value == DamageCause).Key;
            if (DamageType != KeywordSymbol.invalid)
                result += " type: " + VoiceCommandCompiler.KeywordStringToSymbol.First(kv => kv.Value == DamageType).Key;
            result += $"; end zone: {ShouldEndZone.ToString(CultureInfo.InvariantCulture)}";
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SetZonesDetailAction other))
                return false;
            return other.Lanes.SetEquals(Lanes) && other.DamageCause == DamageCause && other.DamageType == DamageType && other.ShouldEndZone == ShouldEndZone;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Lanes, DamageCause, DamageType, ShouldEndZone);
        }
    }
}
