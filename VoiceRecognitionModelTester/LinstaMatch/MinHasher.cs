// Based on https://doi.org/10.1007/s10115-018-1199-5

using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VoiceRecogEvalServer.LinstaMatch
{
    public class MinHasher<T>
    {
        private double AccaptableFalseNegativeRate { get; }
        private int SignatureSize { get; }
        private int[] MinHashSeeds { get; }
        private double SimilarityThreshold { get; }
        public int BandHeight { get; }
        private int BandCount { get; }
        private int BucketSizeLimit { get; }
        public MinHasher(int signatureSize, double similarityThreshold, int bucketSizeLimit = 100, double accaptableFalseNegativeRate = 0.05)
        {
            SignatureSize = signatureSize;
            SimilarityThreshold = similarityThreshold;
            BucketSizeLimit = bucketSizeLimit;
            AccaptableFalseNegativeRate = accaptableFalseNegativeRate;

            BandHeight = CalculateBandHeight(SimilarityThreshold, signatureSize);
            BandCount = SignatureSize / BandHeight;

            MinHashSeeds = GenerateMinhashSeeds(signatureSize);
        }

        public int CalculateBandHeight(double similarityThreshold, int hashFunctionsCount)
        {
            // see https://doi.org/10.1007/s10115-018-1199-5 Algorithm 1
            for (int i = 1; i <= hashFunctionsCount; i++)
            {
                int b = hashFunctionsCount / i;
                double prob = 1.0 - (double)Math.Pow(1.0 - (double)Math.Pow(similarityThreshold, i), b);
                if (1 - prob > AccaptableFalseNegativeRate)
                {
                    return i - 1;
                }
            }
            return hashFunctionsCount;
        }

        private int[] GenerateMinhashSeeds(int signatureSize)
        {
            var minHashKeys = new HashSet<int>();
            Random r = new Random();
            while (minHashKeys.Count < signatureSize)
            {
                minHashKeys.Add(r.Next());
            }

            return minHashKeys.Shuffle().ToArray();
        }

        public List<int[]> GenerateMinHashes(List<T[]> wordList)
        {
            var minHashes = new List<int[]>(wordList.Count);
            for (int i = 0; i < wordList.Count; i++)
            {
                minHashes.Add(GetSignature(wordList[i]));
            }

            return minHashes;
        }

        public int[] GetSignature(T[] tokens)
        {
            int[] minHashValues = new int[SignatureSize];
            Array.Fill(minHashValues, int.MaxValue);

            HashSet<T> knownTokens = new HashSet<T>(tokens.Length);
            foreach (var token in tokens)
            {   
                if (knownTokens.Add(token))
                {
                    int tokenHash = token.GetHashCode();
                    for (int i = 0; i < SignatureSize; i++)
                    {
                        int currentHashValue = tokenHash ^ MinHashSeeds[i];
                        if (currentHashValue < minHashValues[i])
                            minHashValues[i] = currentHashValue;
                    }
                }
            }
            return minHashValues;
        }

        public List<HashSet<int>> GenerateBuckets(List<T[]> wordList, List<int[]> minHashes)
        {
            Dictionary<int, HashSet<int>> buckets = new Dictionary<int, HashSet<int>>();
            HashSet<int> bigBuckets = new HashSet<int>();

            for (int key = 0; key < wordList.Count; key++)
            {
                for (int b = 0; b < BandCount; b++)
                {
                    HashCode bandHash = new HashCode();
                    for (int i = 0; i < BandHeight; i++)
                    {
                        bandHash.Add(minHashes[key][b * BandHeight + i]);
                    }
                    var hash = bandHash.ToHashCode();
                    if (bigBuckets.Contains(hash))
                        continue;

                    if (!buckets.ContainsKey(hash))
                        buckets[hash] = new HashSet<int>();
                    
                    buckets[hash].Add(key);
                    if (buckets[hash].Count > BucketSizeLimit)
                    {
                        bigBuckets.Add(hash);
                        buckets.Remove(hash);
                    }
                }
            }
            return buckets.Select(b => b.Value).ToList();
        }

        public void GeneratePairs(List<HashSet<int>> buckets, string outputPath)
        {
            var knownPairs = new HashSet<int>();
            var wr = new StreamWriter(outputPath);

            Console.WriteLine(string.Join("; ", buckets.Select(l => l.Count)));
            foreach (var bucket in buckets)
            {
                if (bucket.Count <= 1)
                    continue;
                Console.Write(bucket.Count + "; ");
                var bucketList = bucket.ToList();

                for (int i = 0; i < bucketList.Count; i++)
                {
                    for (int j = i + 1; j < bucketList.Count; j++)
                    {
                        if (knownPairs.Add(getKeyFromPair(bucketList[i], bucketList[j])))
                        {
                            wr.WriteLine(bucketList[i] + "," + bucketList[j]);
                        }
                    }
                }
            }
            wr.Close();
        }

        public static int getKeyFromPair(object p1, object p2)
        {
            if (p1.ToString().GetHashCode() <= p2.ToString().GetHashCode())
                return HashCode.Combine(p1, p2);
            else
                return HashCode.Combine(p2, p1);
        }
    }
}
