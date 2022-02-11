using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Defines the most basic form of a phrase recognizer, which can only proces Streams into strings.
    /// </summary>
    public interface IPhraseRecognizer
    {
        Task<string> Process(Stream waveformStream);
    }
    /// <summary>
    /// Defines all functions needed for a fully functional phrase recognizer which works with keywords. Also requires the capabilty to generate all possible phrases.
    /// </summary>
    /// <typeparam name="SymbolT">Type of enum which contains all Keywords</typeparam>
    public interface IPhraseRecognizer<SymbolT> : IPhraseRecognizer where SymbolT : Enum
    {
        VoiceAction Compile(List<SymbolT> recognizedKeywords);
        IEnumerable<List<SymbolT>> GenerateAllPhrases();
        IEnumerable<Pronunciation> GetPronunciations(string word);
        IEnumerable<List<byte>> GetPronunciations(List<SymbolT> symbols);
        List<string> GetStringRepresentations(SymbolT symbol);
    }
}
