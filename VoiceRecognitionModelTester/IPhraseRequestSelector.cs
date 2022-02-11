using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Defines methods which corresponds to phrase request selection.
    /// </summary>
    public interface IPhraseRequestSelector
    {
        bool IsRequestedPhrase(string phrase);
        string GetNextRequestedPhrase();
    }
}
