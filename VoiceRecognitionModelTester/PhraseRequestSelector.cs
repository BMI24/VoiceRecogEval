using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Contains all parameters needed for PhraseSelection.
    /// </summary>
    public struct PhraseRequestSelectionParameters
    {
        public int LSHMaxBucketSize { get; set; }
        public int PhrasePairMinHeapSize { get; set; }
        public int PhrasesSetSize { get; set; }
        public int ShinglePartCount { get; set; }
        public int MinHashSignatureSize { get; set; }
        public double LSHSimilarityThreshold { get; set; }
        public double LSHAllowedFalseNegativeRate { get; set; }
    }

    /// <summary>
    /// Default implementation of <see cref="IPhraseRequestSelector"/> which selects a set of probably confusable phrases 
    /// using locality-sensitive hashing provided by <see cref="NearestNeighbourService"/> but also returns single words for requests.
    /// </summary>
    /// <typeparam name="SymbolT">Type of used symbols in phrase recognition</typeparam>
    public class PhraseRequestSelector<SymbolT> : IPhraseRequestSelector where SymbolT : Enum
    {
        PhraseRequestSelectionParameters Parameters;
        List<string> PhraseRequestSet;
        List<string> WordRequestSet;
        IPhraseRecognizer<SymbolT> PhraseRecognizer;
        public PhraseRequestSelector(IPhraseRecognizer<SymbolT> phraseRecognizer, PhraseRequestSelectionParameters parameters)
        {
            PhraseRecognizer = phraseRecognizer;
            Parameters = parameters;

            GenerateRequestSet();
        }

        private void GenerateRequestSet()
        {
            var outputDirectory = "outputlsh";
            var topPairsFilename = "toppairs.csv";
            var topPairsPath = Path.Combine(outputDirectory, topPairsFilename);
            if (!File.Exists(topPairsPath))
            {
                Stopwatch x = new Stopwatch();
                x.Start();
                new NearestNeighbourService().LSH(PhraseRecognizer, outputDirectory, topPairsFilename, lshSimilarityThreshold: Parameters.LSHSimilarityThreshold, 
                    minhashSignatureSize: Parameters.MinHashSignatureSize, shinglePartCount: Parameters.ShinglePartCount, lshMaxBucketSize: Parameters.LSHMaxBucketSize,
                    lshAcceptableFalseNegativeRate: Parameters.LSHAllowedFalseNegativeRate);
                Console.WriteLine($"Current:{DateTime.Now} LSH took:{x.Elapsed}");
            }

            var interestingPhrases = new List<string>();
            var interesingPhrasesSet = new HashSet<string>();
            using (var sr = new StreamReader(topPairsPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var lineSplit = line.Split(';');
                    
                    var phrase1 = lineSplit[2];
                    if (interesingPhrasesSet.Add(phrase1))
                        interestingPhrases.Add(phrase1);

                    var phrase2 = lineSplit[3];
                    if (interesingPhrasesSet.Add(phrase2))
                        interestingPhrases.Add(phrase2);
                }
            }

            PhraseRequestSet = interestingPhrases.Take(Parameters.PhrasesSetSize).ToList();
            WordRequestSet = Enum.GetValues(typeof(SymbolT)).Cast<SymbolT>().SelectMany(PhraseRecognizer.GetStringRepresentations).ToList();
        }

        Random Random = new Random();
        public string GetNextRequestedPhrase()
        {
            return Random.NextEntry(Random.NextBool() ? PhraseRequestSet : WordRequestSet);
        }

        public bool IsRequestedPhrase(string phrase)
        {
            return WordRequestSet.Contains(phrase) || PhraseRequestSet.Contains(phrase);
        }
    }
}
