using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class FieldMAppPhraseRecognizer : IPhraseRecognizer<KeywordSymbol>
    {
        SpeechRecognizer SpeechRecognizer;
        public FieldMAppPhraseRecognizer()
        {
            SpeechRecognizer = new SpeechRecognizer();
        }

        public VoiceAction Compile(List<KeywordSymbol> recognizedKeywords)
        {
            return VoiceCommandCompiler.Compile(recognizedKeywords);
        }

        public IEnumerable<List<KeywordSymbol>> GenerateAllPhrases()
        {
            return VoiceCommandCompiler.GenerateAllPhrases();
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
