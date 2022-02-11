using Microsoft.VisualBasic.CompilerServices;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using VoiceRecogEvalServer.FieldMAppPhraseRecognition;
using static VoiceRecogEvalServer.FieldMAppPhraseRecognition.SpeechRecognizer;
using static VoiceRecogEvalServer.FieldMAppPhraseRecognition.VoiceCommandCompiler;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public class PronunciationService
    {
        static Dictionary<string, PhoneIdentifier> KaldiRepresentationToPhoneIdentifier = new Dictionary<string, PhoneIdentifier>()
        {
            { " ", new PhoneIdentifier(0) }
        };
        public static Dictionary<PhoneIdentifier, string> PhoneIdentifierToKaldiRepresentation = new Dictionary<PhoneIdentifier, string>()
        {
            { new PhoneIdentifier(0), " " }
        };

        public static int PhoneIdentifierCount => KaldiRepresentationToPhoneIdentifier.Count;

        static PhoneIdentifier GetIdentifier(string kaldiPhoneString)
        {
            if (!KaldiRepresentationToPhoneIdentifier.ContainsKey(kaldiPhoneString))
            {
                var phoneIdentifier = new PhoneIdentifier(PhoneIdentifierCount);
                KaldiRepresentationToPhoneIdentifier.Add(kaldiPhoneString, phoneIdentifier);
                PhoneIdentifierToKaldiRepresentation.Add(phoneIdentifier, kaldiPhoneString);
            }

            return KaldiRepresentationToPhoneIdentifier[kaldiPhoneString];
        }

        static Dictionary<string, List<Pronunciation>> WordToPhones = GetPhones(AcceptedWords);

        public static Dictionary<string, List<Pronunciation>> GetPhones(List<string> words)
        {
            var result = new Dictionary<string, List<Pronunciation>>();

            var pathToDict = Path.Combine("voskModelDe", "lexicon.txt");

            using (var fileReader = new StreamReader(pathToDict))
            {
                string line;
                while ((line = fileReader.ReadLine()) != null)
                {
                    var lineParts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var firstPart = lineParts[0];
                    if (firstPart[0] == '<' || firstPart[0] == '!')
                        continue;

                    var word = firstPart.Split('_', StringSplitOptions.RemoveEmptyEntries)[0];
                    if (!words.Contains(word))
                        continue;

                    var phones = new List<PhoneIdentifier>();
                    for (int i = 1; i < lineParts.Length; i++)
                    {
                        phones.Add(GetIdentifier(lineParts[i]));
                    }

                    if (!result.ContainsKey(word))
                        result[word] = new List<Pronunciation>();

                    result[word].Add(new Pronunciation(phones));
                }
            }
            var wordsWithoutProns = words.Where(w => w != "[unk]").Except(result.Select(kv => kv.Key)).ToList();
            if (wordsWithoutProns.Any())
                throw new Exception("Not all words have pronunciations in lexicon.");
            return result;
        }

        static List<Pronunciation> SilencePronounciationList = new List<Pronunciation> { new Pronunciation(new List<PhoneIdentifier> { new PhoneIdentifier(0) }) };
        public static IEnumerable<List<byte>> GetPronunciations(List<KeywordSymbol> symbols)
        {
            List<List<Pronunciation>> options = new List<List<Pronunciation>>();
            bool first = true;
            foreach (var symbol in symbols)
            {
                if (!first)
                    options.Add(SilencePronounciationList);
                first = false;

                var prons = GetPronunciation(symbol).ToList();
                if (prons.Count == 0)
                    throw new Exception($"Symbol {symbol} has no pronounciation.");
                options.Add(prons);
            }


            IEnumerable<List<Pronunciation>> getPronunciationLists()
            {
                Stack<List<Pronunciation>> stack = new Stack<List<Pronunciation>>();
                stack.Push(new List<Pronunciation>());
                while (stack.Count > 0)
                {
                    var pronPart = stack.Pop();
                    foreach (var nextPart in options[pronPart.Count])
                    {
                        var newPronPart = options[pronPart.Count].Count == 1 ? pronPart : new List<Pronunciation>(pronPart);
                        newPronPart.Add(nextPart);
                        if (newPronPart.Count == options.Count)
                            yield return newPronPart;
                        else
                            stack.Push(newPronPart);
                    }
                }
            }

            foreach (var pronunciation in getPronunciationLists())
            {
                yield return pronunciation.SelectMany(p => p.Phones).Select(p => (byte)p.Index).ToList();
            }
        }

        public static IEnumerable<Pronunciation> GetPronunciation(KeywordSymbol symbol)
        {
            var words = SymbolToKeywordStrings[symbol];
            return words.SelectMany(word => WordToPhones[word]);
        }
        public static IEnumerable<Pronunciation> GetPronunciation(string word)
        {
            if (WordToPhones.TryGetValue(word, out var phones))
                return phones;
            return Enumerable.Empty<Pronunciation>();
        }
    }
}
