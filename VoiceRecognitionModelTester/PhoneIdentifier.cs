using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Provides a wrapper around an int which reperesents the phone.
    /// </summary>
    public struct PhoneIdentifier : IEquatable<PhoneIdentifier>
    {
        public int Index;

        public PhoneIdentifier(int index)
        {
            Index = index;
        }

        public bool Equals([AllowNull] PhoneIdentifier other)
        {
            return Index == other.Index;
        }

        public override string ToString()
        {
            return Index.ToString();
        }
    }
}
