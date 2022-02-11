using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class FieldMapWordRecognizer : IPhraseRecognizer<KeywordSymbol>
    {
        SpeechRecognizer SpeechRecognizer;
        public FieldMapWordRecognizer()
        {
            SpeechRecognizer = new SpeechRecognizer();
        }
        public class SymbolVoiceAction : VoiceAction 
        {
            KeywordSymbol Symbol;

            public SymbolVoiceAction(KeywordSymbol symbol)
            {
                this.Symbol = symbol;
            }

            public override bool Equals(object obj)
            {
                return obj is SymbolVoiceAction action &&
                       Symbol == action.Symbol;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Symbol);
            }
        }

        public VoiceAction Compile(List<KeywordSymbol> recognizedKeywords)
        {
            return new SymbolVoiceAction(recognizedKeywords[0]);
        }

        public IEnumerable<List<KeywordSymbol>> GenerateAllPhrases()
        {
            return VoiceCommandCompiler.KeywordStringToSymbol.Values.Distinct().Where(v => v != KeywordSymbol.unk).Select(s => new List<KeywordSymbol> { s });
        }

        public IEnumerable<Pronunciation> GetPronunciations(string word)
        {
            return PronunciationService.GetPronunciation(word);
        }

        public IEnumerable<List<byte>> GetPronunciations(List<KeywordSymbol> symbols)
        {
            return PronunciationService.GetPronunciations(symbols);
        }

        List<string> EmptyStringList = new List<string>();
        public List<string> GetStringRepresentations(KeywordSymbol symbol)
        {
            if (VoiceCommandCompiler.SymbolToKeywordStrings.TryGetValue(symbol, out var stringReprs))
                return stringReprs;
            return EmptyStringList;
        }

        public async Task<string> Process(Stream waveformStream)
        {
            await SpeechRecognizer.LoadTask;
            var result = await SpeechRecognizer.Process(waveformStream);
            return result.Result;
        }
    }
}
