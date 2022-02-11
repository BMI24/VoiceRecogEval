using MoreLinq;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VoiceRecogEvalServer.LinstaMatch;

namespace VoiceRecogEvalServer
{
    /// <summary>
    /// Provides multiple Methods which help with finding near duplicates, mostly useful for strings.
    /// </summary>
    public class NearestNeighbourService
    {
        /// <summary>
        /// Performs locality sensitive hashing on the phrases provided by a <see cref="IPhraseRecognizer{SymbolT}"/> and saves the final result to a csv file
        /// </summary>
        public void LSH<SymbolT>(IPhraseRecognizer<SymbolT> phraseRecognizer, string outputDirPath, string topPairsFilename, double lshSimilarityThreshold, int minhashSignatureSize, int shinglePartCount, int lshMaxBucketSize, double lshAcceptableFalseNegativeRate) where SymbolT:Enum
        {
            outputDirPath = outputDirPath ?? throw new ArgumentNullException(nameof(outputDirPath));
            if (Directory.Exists(outputDirPath))
            {
                Console.WriteLine($"{DateTime.Now}: Using existing {outputDirPath} as output directory.");
            }
            else
            {
                Directory.CreateDirectory(outputDirPath);
                Console.WriteLine($"{DateTime.Now}: Created {outputDirPath} as output directory.");
            }
            
            Stopwatch x = new Stopwatch();
            x.Start();
            var pairsOutputPath = Path.Combine(outputDirPath, "sim_output.txt");
            if (File.Exists(pairsOutputPath))
            {
                Console.WriteLine($"{DateTime.Now}: {pairsOutputPath} already exists, skipping this part.");
            }
            else
            {
                var minHasher = new MinHasher<uint>(signatureSize: minhashSignatureSize, similarityThreshold:lshSimilarityThreshold, 
                    bucketSizeLimit: lshMaxBucketSize, accaptableFalseNegativeRate: lshAcceptableFalseNegativeRate);

                List<uint[]> wordList = phraseRecognizer.GenerateAllPhrases()
                    .SelectMany(phraseRecognizer.GetPronunciations)
                    .Select(p => GetNShingles(shinglePartCount, p))
                    .Select(p => p.ToArray()).ToList();

                Console.WriteLine($"{DateTime.Now}: WordListConstruction took:{x.Elapsed}");

                x.Restart();
                List<int[]> minHashes = minHasher.GenerateMinHashes(wordList);
                Console.WriteLine($"{DateTime.Now}: CreateMinHashCollection took:{x.Elapsed}");

                x.Restart();
                List<HashSet<int>> lshBuckets = minHasher.GenerateBuckets(wordList, minHashes);
                Console.WriteLine($"{DateTime.Now}: CreateBandBuckets took:{x.Elapsed}");

                x.Restart();
                minHasher.GeneratePairs(lshBuckets, pairsOutputPath);
                Console.WriteLine($"{DateTime.Now}: GenerateVertexPairs took:{x.Elapsed}");
            }


            var editDistanceOutputPath = Path.Combine(outputDirPath, "sim_output_levenshtein.txt");
            if (File.Exists(editDistanceOutputPath))
            {
                Console.WriteLine($"{DateTime.Now}: {editDistanceOutputPath} already exists, skipping this part.");
            }
            else
            {
                x.Restart();
                CalculateEditDistance(phraseRecognizer, pairsOutputPath, editDistanceOutputPath);
                Console.WriteLine($"{DateTime.Now}: CalculateEditDistance took:{x.Elapsed}");
            }

            var topPairsOutputPath = Path.Combine(outputDirPath, topPairsFilename ?? "sim_output_topPairs.txt");
            if (File.Exists(topPairsOutputPath))
            {
                Console.WriteLine($"{DateTime.Now}: {topPairsOutputPath} already exists, skipping this part.");
            }
            else
            {
                x.Restart();
                FindTop(phraseRecognizer, 10000, editDistanceOutputPath, topPairsOutputPath);
                Console.WriteLine($"{DateTime.Now}: FindTop took:{x.Elapsed}");
            }
        }

        public static List<uint> GetNShingles(int n, IEnumerable<byte> pronounciation)
        {
            var trigrams = new List<uint>();
            uint trigram = 0;
            int toSkip = n;
            foreach (var phone in pronounciation)
            {
                trigram = trigram << 8 | phone;
                if (toSkip == 0)
                    trigrams.Add(trigram);
                else
                    toSkip--;
            }
            trigrams.TrimExcess();
            return trigrams;
        }

        private void FindTop<T>(IPhraseRecognizer<T> phraseRecognizer, int n, string inputPath, string topPairsOutputPath) where T:Enum
        {
            var pronIdToInfo = phraseRecognizer.GenerateAllPhrases()
                .SelectMany(phrase => phraseRecognizer.GetPronunciations(phrase).Select(pron => (pron, phrase)))
                .ToList();

            var pronToStringRepr = new Dictionary<string, string>();
            foreach (var stringRepr in Enum.GetValues(typeof(T)).Cast<T>().SelectMany(phraseRecognizer.GetStringRepresentations))
            {
                foreach (var pron in phraseRecognizer.GetPronunciations(stringRepr))
                {
                    var phoneByteList = pron.Phones.Select(phone => (char)phone.Index).ToArray();
                    var phoneByteListStr = new string(phoneByteList);
                    pronToStringRepr[phoneByteListStr] = stringRepr;
                }
            }

            string phraseToString(List<byte> pron)
            {
                var pronStr = new string(pron.Select(p => (char)p).ToArray());
                var wordProns = pronStr.Split((char)0);
                return string.Join(' ', wordProns.Select(w => pronToStringRepr[w]));
                //return string.Join(' ', info.phrase.Select(symbol => phraseRecognizer.GetStringRepresentation(symbol).FirstOrDefault(s => phraseRecognizer.GetPronunciations(s) == symbol) phraseRecognizer.GetStringRepresentation));
            }
            var heap = new FastPriorityQueue<SimilarityPair>(n + 5);

            var diffEngine = DiffMatchPatch.DiffMatchPatchModule.Default;

            using (StreamReader sr = new StreamReader(inputPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var simPair = new SimilarityPair();
                    var lineSplit = line.Split(';');
                    simPair.Pron1Id = int.Parse(lineSplit[0]);
                    simPair.Pron2Id = int.Parse(lineSplit[1]);

                    simPair.EditDistance = int.Parse(lineSplit[2]);
                    simPair.NormalizedEditDistance = double.Parse(lineSplit[3]);

                    var symbolList1 = pronIdToInfo[simPair.Pron1Id].phrase;
                    var symbolList2 = pronIdToInfo[simPair.Pron2Id].phrase;
                    var diff = diffEngine.DiffMain(string.Join("", symbolList1.Select(p => (int)(object)p)), string.Join("", symbolList2.Select(p => (int)(object)p)));

                    if (Helpers.LevenshteinDistance(symbolList1, symbolList2) > 1)
                    {
                        var action1 = phraseRecognizer.Compile(symbolList1);
                        var action2 = phraseRecognizer.Compile(symbolList2);


                        if (!action1.Equals(action2))
                        {
                            heap.Enqueue(simPair, -simPair.EditDistance);
                            if (heap.Count > n)
                                heap.Dequeue();
                        }
                    }

                }
            }

            string diffToString(List<DiffMatchPatch.Diff> diffs)
            {
                string result = string.Empty;
                string prevSymbol = null;
                foreach (var diff in diffs)
                {
                    if (diff.Operation.IsEqual)
                    {
                        if (prevSymbol != null)
                        {
                            result += '(' + string.Join(" ", prevSymbol.Select(c => ((T)(object)(int)c).ToString())) + ") ";
                            prevSymbol = null;
                        }
                    }
                    else if (diff.Operation.IsInsert || diff.Operation.IsDelete)
                    {
                        if (prevSymbol == null)
                            prevSymbol = diff.Text;
                        else
                        {
                            var prevHash = prevSymbol.GetHashCode();
                            var currHash = diff.Text.GetHashCode();
                            if (currHash >= prevHash)
                                result += '(' 
                                    + string.Join(" ", prevSymbol.Select(c => ((T)(object)(int)c).ToString())) 
                                    + ")/(" 
                                    + string.Join(" ", diff.Text.Select(c => ((T)(object)(int)c).ToString())) 
                                    + ") ";
                            else
                                result += '(' 
                                    + string.Join(" ", diff.Text.Select(c => ((T)(object)(int)c).ToString())) 
                                    + ")/(" 
                                    + string.Join(" ", prevSymbol.Select(c => ((T)(object)(int)c).ToString())) 
                                    + ") ";
                            prevSymbol = null;
                        }
                    }
                }
                if (prevSymbol != null)
                {
                    result += '(' + string.Join(" ", prevSymbol.Select(c => ((T)(object)(int)c).ToString())) + ") ";
                }
                return result;
            }


            if (!File.Exists(topPairsOutputPath))
            {
                var shortestPairsWithDistinctDifferences = heap.Select(p =>
                {
                    var phrase1Info = pronIdToInfo[p.Pron1Id];
                    var phrase2Info = pronIdToInfo[p.Pron2Id];

                    // tbh i hate the casting needed for this. but i need a IEnumerable<char> here
                    // C# does not recognize that Enums can be casted to numerics
                    // and for some reason (char)(object) throws a System.InvalidCastException

                    var diff = diffEngine.DiffMain(string.Join("", phrase1Info.phrase.Select(p => (char)(int)(object)p)), string.Join("", phrase2Info.phrase.Select(p => (char)(int)(object)p)));

                    return new
                    {
                        Pron1Id = p.Pron1Id,
                        Pron2Id = p.Pron2Id,
                        Phrase1 = string.Join(' ', phraseToString(phrase1Info.pron)),
                        Phrase2 = string.Join(' ', phraseToString(phrase2Info.pron)),
                        NormalizedEditDistance = p.NormalizedEditDistance,
                        EditDistance = p.EditDistance,
                        StringDiff = diffToString(diff)
                    };
                })
                        .GroupBy(e => e.StringDiff, e => e)
                        .Select(
                            g => g.Select(x => x)
                                .MinBy(x => x.Phrase1.Length))
                        .OrderBy(g => g.EditDistance)
                        .ToList();

                using (var file = File.CreateText(topPairsOutputPath))
                {
                    foreach (var p in shortestPairsWithDistinctDifferences)
                    {
                        var arr = new object[] { p.Pron1Id, p.Pron2Id, p.Phrase1, p.Phrase2, p.NormalizedEditDistance, p.EditDistance, p.StringDiff };
                        file.WriteLine(string.Join(";", arr));
                    }
                }
            }
            Console.WriteLine($"Top{n}-heap size: " + heap.Count);
        }

        class SimilarityPair : FastPriorityQueueNode
        {
            public int Pron1Id;
            public int Pron2Id;
            public int EditDistance;
            public double NormalizedEditDistance;
        }

        private static void CalculateEditDistance<T>(IPhraseRecognizer<T> phraseRecognizer, string inputPath, string outputPath) where T:Enum
        {
            var phrases = phraseRecognizer.GenerateAllPhrases()
                .SelectMany(phraseRecognizer.GetPronunciations)
                .Select(Enumerable.ToArray)
                .Select(pron => string.Join(string.Empty, pron.Select(phon => (char)phon)))
                .ToList();
            var simPairs = new List<SimilarityPair>();
            using (StreamReader sr = new StreamReader(inputPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var simPair = new SimilarityPair();
                    var lineSplit = line.Split(",");
                    simPair.Pron1Id = int.Parse(lineSplit[0]);
                    simPair.Pron2Id = int.Parse(lineSplit[1]);

                    var phrase1 = phrases[simPair.Pron1Id];
                    var phrase2 = phrases[simPair.Pron2Id];
                    simPair.EditDistance = Fastenshtein.Levenshtein.Distance(phrase1, phrase2);
                    float l = Math.Max(phrase1.Length, phrase2.Length);
                    simPair.NormalizedEditDistance = (l - simPair.EditDistance) / l;
                    simPairs.Add(simPair);
                }
            }
            using var file = File.CreateText(outputPath);
            foreach (var p in simPairs)
            {
                var arr = new object[] { p.Pron1Id, p.Pron2Id, p.EditDistance, p.NormalizedEditDistance };
                file.WriteLine(string.Join(";", arr));
            }
        }

        /// <summary>
        /// Applies High-Value Token-Blocking <see cref="https://doi.org/10.1145/3450527"/> 
        /// Works best with higher token-count, unsuitable for our token count of 40.
        /// </summary>
        /// <typeparam name="T">Type of token</typeparam>
        /// <param name="records">Records in standardized form</param>
        public void HVTB(IEnumerable<List<int>> records)
        {
            Dictionary<int, int> frequencies = new Dictionary<int, int>();
            List<HashSet<int>> tokenizedRecords = new List<HashSet<int>>();
            Dictionary<int, HashSet<int>> blocks = new Dictionary<int, HashSet<int>>();
            int recordsCount = 0;
            foreach (var record in records)
            {
                var tokens = new HashSet<int>();
                foreach (var token in record)
                {
                    if (!tokens.Contains(token))
                    {
                        blocks[token] = new HashSet<int>();
                        tokens.Add(token);
                        frequencies[token] = (frequencies.TryGetValue(token, out int frequency) ? frequency : 0) + 1;
                    }
                }
                tokenizedRecords.Add(tokens);
                recordsCount++;
            }

            Dictionary<int, double> idfs = new Dictionary<int, double>();
            foreach (var kv in frequencies)
            {
                idfs[kv.Key] = Math.Log(recordsCount / (kv.Value + 1d));
            }

            foreach (var (record, index) in records.Select((r, i) => (r, i)))
            {
                var nonUniqueTfidfs = record.GroupBy(r => r)
                    .Where(g  => frequencies[g.Key] > 1)
                    .Select(n => new { Token = n.Key, tfidf = record.Count / n.Count()*idfs[n.Key] })
                    .ToArray();
                if (nonUniqueTfidfs.Length > 0)
                {
                    double tfIdfAvg = nonUniqueTfidfs.Average(f => f.tfidf);
                    foreach (var token in nonUniqueTfidfs.Where(f => f.tfidf > tfIdfAvg))
                    {
                        blocks[token.Token].Add(index);
                    }
                }
            }

            Dictionary<Tuple<int, int>, int> pairWeights = new Dictionary<Tuple<int, int>, int>();
            foreach (var block in blocks)
            {
                var combinations = from item1 in block.Value
                                   from item2 in block.Value
                                   where item1 < item2
                                   select Tuple.Create(item1, item2);
                foreach (var combination in combinations)
                {
                    if (!pairWeights.ContainsKey(combination))
                    {
                        pairWeights[combination] = tokenizedRecords[combination.Item1].Union(tokenizedRecords[combination.Item2]).Count();
                    }
                }
            }
            var avgPairWeight = pairWeights.Values.Average();
            foreach (var uselessPair in pairWeights.Where(kv => kv.Value < avgPairWeight).ToList())
            {
                pairWeights.Remove(uselessPair.Key);
            }
        }
    }
}
