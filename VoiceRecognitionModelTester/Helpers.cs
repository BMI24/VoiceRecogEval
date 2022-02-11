using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer
{
    public static class Helpers
    {
        /// <summary>
        /// Extension Method providing the IndexOf method for <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="IReadOnlyList{T}"/></typeparam>
        /// <param name="list">List in which the given element will be searched for</param>
        /// <param name="element">Element which wil be looked for</param>
        /// <returns>Index of element in list or (if the element is not in list) -1.</returns>
        public static int IndexOf<T>(this IReadOnlyList<T> list, T element)
        {
            var listCount = list.Count;
            for (int i = 0; i < listCount; i++)
            {
                if (list[i].Equals(element))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Calculates the levenshtein distance of two Lists.
        /// Taken from https://www.dotnetperls.com/levenshtein
        /// </summary>
        public static int LevenshteinDistance<T>(IList<T> s, IList<T> t)
        {
            int n = s.Count;
            int m = t.Count;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) { }

            for (int j = 0; j <= m; d[0, j] = j++) { }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = t[j - 1].Equals(s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        /// <summary>
        /// Calculates the levenshtein distance of two strings.
        /// Taken from https://www.dotnetperls.com/levenshtein
        /// </summary>
        public static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Verify arguments.
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Initialize arrays.
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Begin looping.
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    // Compute cost.
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            // Return cost.
            return d[n, m];
        }

        public static IEnumerable<(T a, T b)> GenerateAllPairs<T>(this List<T> listA, List<T> listB)
        {
            for (int i = 0; i < listA.Count; i++)
            {
                for (int j = 0; j < listB.Count; j++)
                {
                    yield return (listA[i], listB[j]);
                }
            }
        }

        public static IEnumerable<(T a, T b)> GenerateAllPairs<T>(this List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = i + 1; j < list.Count; j++)
                {
                    yield return (list[i], list[j]);
                }
            }
        }
        
        public static T NextEnum<T>(this Random r) where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(r.Next(values.Length));
        }

        public static bool NextBool(this Random r)
        {
            return r.Next(2) == 1;
        }

        public static T NextEntry<T>(this Random r, T[] array)
        {
            return array[r.Next(array.Length)];
        }

        public static T NextEntry<T>(this Random r, List<T> list)
        {
            return list[r.Next(list.Count)];
        }

        /// <summary>
        /// Selects an element randomly, based on the supplied weights 
        /// </summary>
        /// <param name="r">Instance of the <see cref="Random"/> Class, providing the randomness</param>
        /// <param name="list">List of items from which one will be selected</param>
        /// <param name="weights">The corresponding weights. Sum should be 1</param>
        /// <returns></returns>
        public static T NextEntry<T>(this Random r, IList<T> list, IList<double> weights)
        {
            if (list.Count != weights.Count)
                throw new ArgumentException("Input list and weights must have the same count.");

            var target = r.NextDouble();
            double currentSum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (currentSum + weights[i] > target)
                    return list[i];
                currentSum += weights[i];
            }
            throw new ArgumentException("The sum of all weights is less then 0");
        }
    }
}
