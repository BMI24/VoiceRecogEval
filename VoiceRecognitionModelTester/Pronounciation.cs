using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Wrapper for a list of PhoneIdentifiers.
    /// </summary>
    public class Pronunciation
    {
        public List<PhoneIdentifier> Phones;

        public Pronunciation(List<PhoneIdentifier> phones)
        {
            Phones = phones;
        }

        public static implicit operator List<PhoneIdentifier>(Pronunciation p)
        {
            return p.Phones;
        }
        public static implicit operator Pronunciation(List<PhoneIdentifier> p)
        {
            return new Pronunciation(p);
        }

        public override string ToString()
        {
            return '{' + string.Join(", ", Phones) + '}';
        }
    }
}
