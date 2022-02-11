using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public static class VoiceCommandCompiler
    {
        static KeywordSymbol[] DamageTypes = new[] { KeywordSymbol.gering, KeywordSymbol.mittel, KeywordSymbol.hoch };
        static KeywordSymbol[] DamageCauses = new[]
        {
            KeywordSymbol.hang, KeywordSymbol.nass, KeywordSymbol.maus, KeywordSymbol.wild, KeywordSymbol.trocken, KeywordSymbol.sand, KeywordSymbol.kuppe,
            KeywordSymbol.ton, KeywordSymbol.verdichtung, KeywordSymbol.wende, KeywordSymbol.waldrand, KeywordSymbol.lehm
        };

        public static bool IsNumber(this KeywordSymbol symbol) => NumberSymbols.Contains(symbol);
        public static int ToNumber(this KeywordSymbol symbol) => NumberSymbols.IndexOf(symbol);
        public static bool TryToNumber(this KeywordSymbol symbol, out int digit)
        {
            digit = -1;
            if (!symbol.IsNumber())
                return false;

            digit = symbol.ToNumber();
            return true;
        }
        public static bool IsDamageType(this KeywordSymbol symbol) => DamageTypes.Contains(symbol);
        public static bool IsDamageCause(this KeywordSymbol symbol) => DamageCauses.Contains(symbol);

        public static readonly Dictionary<string, KeywordSymbol> KeywordStringToSymbol = new Dictionary<string, KeywordSymbol>
        {
            { "anfang", KeywordSymbol.anfang },
            { "start", KeywordSymbol.anfang },
            { "stopp", KeywordSymbol.ende },
            { "abbrechen", KeywordSymbol.abbrechen },
            { "gering", KeywordSymbol.gering },
            { "mittel", KeywordSymbol.mittel },
            { "hoch", KeywordSymbol.hoch },
            { "hang", KeywordSymbol.hang },
            { "nass", KeywordSymbol.nass },
            { "nässe", KeywordSymbol.nass },
            { "maus", KeywordSymbol.maus },
            { "mäuse", KeywordSymbol.maus },
            { "wild", KeywordSymbol.wild },
            { "trocken", KeywordSymbol.trocken },
            { "sand", KeywordSymbol.sand },
            { "kuppe", KeywordSymbol.kuppe },
            { "ton", KeywordSymbol.ton },
            { "verdichtung", KeywordSymbol.verdichtung },
            { "wald", KeywordSymbol.waldrand },
            { "wende", KeywordSymbol.wende },
            { "spur", KeywordSymbol.spur },
            { "[unk]", KeywordSymbol.unk },
            { "eins", KeywordSymbol.number1 },
            { "zwei", KeywordSymbol.number2 },
            { "drei", KeywordSymbol.number3 },
            { "vier", KeywordSymbol.number4 },
            { "fünf", KeywordSymbol.number5 },
            { "sechs", KeywordSymbol.number6 },
            { "zone", KeywordSymbol.zone },
            { "lehm", KeywordSymbol.lehm }
        };

        public static readonly Dictionary<KeywordSymbol, List<string>> SymbolToKeywordStrings = KeywordStringToSymbol
            .GroupBy(p => p.Value)
            .ToDictionary(g => g.Key, g => g.Select(pp => pp.Key).ToList());

        readonly static KeywordSymbol[] NumberSymbols = new[]
        {
            KeywordSymbol.number0, KeywordSymbol.number1, KeywordSymbol.number2, KeywordSymbol.number3, KeywordSymbol.number4,
            KeywordSymbol.number5, KeywordSymbol.number6
        };

        public readonly static List<string> KeywordStrings =
            KeywordStringToSymbol
            .Select(kv => kv.Key)
            .ToList();

        public static VoiceAction Compile(List<string> recognizedKeywords) => Parse(Scan(recognizedKeywords));
        public static VoiceAction Compile(List<KeywordSymbol> recognizedKeywords) => Parse(recognizedKeywords);

        static List<KeywordSymbol> Scan(List<string> recognizedKeywords) => recognizedKeywords.Select(k => KeywordStringToSymbol[k]).ToList();

        class SymbolStreamAccessor
        {
            public SymbolStreamAccessor(List<KeywordSymbol> symbols)
            {
                Symbols = symbols;
                Index = 0;
            }
            List<KeywordSymbol> Symbols;
            int Index;
            public KeywordSymbol Peek()
            {
                if (Index == Symbols.Count)
                    return KeywordSymbol.endOfStream;

                return Symbols[Index];
            }
            public KeywordSymbol Next()
            {
                if (Index == Symbols.Count)
                    return KeywordSymbol.endOfStream;

                return Symbols[Index++];
            }
        }

        public static IEnumerable<List<KeywordSymbol>> GenerateAllPhrases()
        {
            LinkedList<KeywordSymbol> symbols = new LinkedList<KeywordSymbol>();

            // very weird code. Use yield return to abuse c# flow control

            IEnumerable<object> required(params KeywordSymbol[] newSymbols)
            {
                foreach (var symbol in newSymbols)
                {
                    symbols.AddLast(symbol);
                }
                yield return null;
                foreach (var symbol in newSymbols)
                {
                    symbols.RemoveLast();
                }
            }

            IEnumerable<object> optional(params KeywordSymbol[] newSymbols)
            {
                foreach (var symbol in newSymbols)
                {
                    symbols.AddLast(symbol);
                }
                yield return null;
                foreach (var symbol in newSymbols)
                {
                    symbols.RemoveLast();
                }
                yield return true;
            }

            IEnumerable<object> repeat(Func<IEnumerable<object>> content)
            {
                foreach (var p in content())
                {
                    yield return null;
                    foreach (var q in repeat(content))
                    {
                        yield return q;
                    }
                }
            }

            IEnumerable<object> program()
            {
                foreach (var p in required(KeywordSymbol.abbrechen))
                {
                    yield return p;
                }
                foreach (var p in zonenStart())
                {
                    yield return p;
                }
                foreach (var p in zonenEnde())
                {
                    yield return p;
                }
                foreach (var p in zonenTypisierung())
                {
                    yield return p;
                }
            }

            IEnumerable<object> zonenTypisierung()
            {
                foreach (var p in zonenTypisierung1())
                {
                    foreach (var q in optional(KeywordSymbol.ende))
                    {
                        yield return q;
                    }
                }
                foreach (var p in zonenTypisierung2())
                {
                    foreach (var q in optional(KeywordSymbol.ende))
                    {
                        yield return q;
                    }
                }
                foreach (var p in zonenTypisierung3())
                {
                    foreach (var q in optional(KeywordSymbol.ende))
                    {
                        yield return q;
                    }
                }
                foreach (var p in zonenTypisierung4())
                {
                    foreach (var q in optional(KeywordSymbol.ende))
                    {
                        yield return q;
                    }
                }
            }

            IEnumerable<object> zonenTypisierung1()
            {
                foreach (var p in spurenAufzaehlung())
                {
                    foreach (var q in zonenAngabe())
                    {
                        yield return q;
                    }
                }
            }

            IEnumerable<object> zonenTypisierung2()
            {
                foreach (var p in zonenAngabe())
                {
                    foreach (var q in spurenAufzaehlung())
                    {
                        yield return q;
                    }
                }
            }

            IEnumerable<object> zonenTypisierung3()
            {
                foreach (var p in minderertragsUrsache())
                {
                    foreach (var q in spurenAufzaehlung())
                    {
                        foreach (var r in minderertragsTyp())
                        {
                            yield return r;
                        }
                    }
                }
            }

            IEnumerable<object> zonenTypisierung4()
            {
                foreach (var p in minderertragsTyp())
                {
                    foreach (var q in spurenAufzaehlung())
                    {
                        foreach (var r in minderertragsUrsache())
                        {
                            yield return r;
                        }
                    }
                }
            }

            IEnumerable<object> zonenAngabe()
            {
                foreach (var p in minderertragsUrsache())
                {
                    foreach (var q in minderertragsTyp())
                    {
                        yield return q;
                    }
                }
                foreach (var p in minderertragsTyp())
                {
                    foreach (var q in minderertragsUrsache())
                    {
                        yield return q;
                    }
                }
                foreach (var p in minderertragsTyp())
                {
                    yield return p;
                }
                foreach (var p in minderertragsUrsache())
                {
                    yield return p;
                }
            }

            IEnumerable<object> minderertragsUrsache()
            {
                KeywordSymbol[] possibleSymbols = { KeywordSymbol.hang, KeywordSymbol.nass, KeywordSymbol.maus, KeywordSymbol.wild, KeywordSymbol.lehm, KeywordSymbol.sand, KeywordSymbol.kuppe, KeywordSymbol.ton, KeywordSymbol.verdichtung, KeywordSymbol.wende };
                foreach (var symbol in possibleSymbols)
                {
                    if (!symbols.Contains(symbol))
                    {
                        foreach (var p in required(symbol))
                        {
                            yield return null;
                        }
                    }
                }
            }

            IEnumerable<object> minderertragsTyp()
            {
                KeywordSymbol[] possibleSymbols = { KeywordSymbol.gering, KeywordSymbol.mittel, KeywordSymbol.hoch };
                foreach (var symbol in possibleSymbols)
                {
                    if (!symbols.Contains(symbol))
                    {
                        foreach (var p in required(symbol))
                        {
                            yield return null;
                        }
                    }
                }
            }


            IEnumerable<object> zonenEnde()
            {
                foreach (var p in required(KeywordSymbol.ende))
                {
                    foreach (var q in spurenAufzaehlung())
                    {
                        yield return q;
                    }
                }
            }

            IEnumerable<object> zonenStart()
            {
                foreach (var p in required(KeywordSymbol.anfang))
                {
                    foreach (var q in spurenAufzaehlung())
                    {
                        yield return q;
                    }
                }

            }

            IEnumerable<object> spurenAufzaehlung()
            {
                foreach (var p in optional(KeywordSymbol.zone))
                {
                    foreach (var q in spurenAufzaehlungTeil())
                    {
                        foreach (var r in repeat(spurenAufzaehlungTeil))
                        {
                            yield return r;
                        }
                    }
                }
            }

            IEnumerable<object> spurenAufzaehlungTeil()
            {
                if (!symbols.Contains(KeywordSymbol.spur))
                {
                    foreach (var p in required(KeywordSymbol.spur))
                    {
                        yield return p;
                    }
                }
                foreach (var p in spurBezeichnung())
                {
                    foreach (var q in repeat(spurBezeichnung))
                    {
                        yield return q;
                    }
                }
            }


            IEnumerable<object> spurBezeichnung()
            {
                KeywordSymbol[] numberSymbols = { KeywordSymbol.number1, KeywordSymbol.number2, KeywordSymbol.number3, KeywordSymbol.number4, KeywordSymbol.number5, KeywordSymbol.number6 };
                foreach (var laneNumber in numberSymbols)
                {
                    if (!symbols.Contains(laneNumber))
                    {
                        foreach (var p in required(laneNumber))
                        {
                            yield return null;
                        }
                    }
                }
            }

            //List<KeywordSymbol> last = null;
            foreach (var p in program())
            {
                var l = symbols.ToList();
                // TODO: The following list occurs multiple times. Why?
                //if (l.Count == 8 && l[0] == KeywordSymbol.wende && l[1] == KeywordSymbol.number6 && l[2] == KeywordSymbol.number5 && l[3] == KeywordSymbol.number4 && l[4] == KeywordSymbol.number3
                //    && l[5] == KeywordSymbol.number2 && l[6] == KeywordSymbol.number1 && l[7] == KeywordSymbol.spur)
                //{
                //
                //}
                yield return l;
                //last = l;
            }

            yield break;
        }
        
        static Dictionary<KeywordSymbol, int> SymbolToLane = new Dictionary<KeywordSymbol, int>
        {
            { KeywordSymbol.spur, 0 },
            { KeywordSymbol.number1, 1 },
            { KeywordSymbol.number2, 2 },
            { KeywordSymbol.number3, 3 },
            { KeywordSymbol.number4, 4 },
            { KeywordSymbol.number5, 5 },
            { KeywordSymbol.number6, 6 },
        };
        static VoiceAction Parse(List<KeywordSymbol> symbols)
        {
            HashSet<LaneDescription> GetZonesIndexList(SymbolStreamAccessor acc)
            {
                if (acc.Peek() == KeywordSymbol.zone)
                    acc.Next();

                HashSet<LaneDescription> result = new HashSet<LaneDescription>();
                while (SymbolToLane.TryGetValue(acc.Peek(), out var laneIndex))
                {
                    acc.Next();
                    result.Add(new LaneDescription(laneIndex));
                }
                return result;
            }

            var accessor = new SymbolStreamAccessor(symbols);
            var firstSymbol = accessor.Peek();

            if (firstSymbol == KeywordSymbol.anfang || firstSymbol == KeywordSymbol.ende)
            {
                ZonesAction action = firstSymbol == KeywordSymbol.anfang ? (ZonesAction)new StartZonesAction() : new EndZonesAction();
                accessor.Next();
                action.Lanes = GetZonesIndexList(accessor);
                if (accessor.Next() != KeywordSymbol.endOfStream)
                    return new InvalidAction();
                return action;
            }
            else if (firstSymbol.IsDamageCause() || firstSymbol.IsDamageType() || firstSymbol == KeywordSymbol.zone || SymbolToLane.ContainsKey(firstSymbol))
            {
                var action = new SetZonesDetailAction();

                void tryReadOneLineDetail()
                {
                    if (accessor.Peek().IsDamageCause())
                        action.DamageCause = accessor.Next();
                    else if (accessor.Peek().IsDamageType())
                        action.DamageType = accessor.Next();
                }

                tryReadOneLineDetail();
                tryReadOneLineDetail();
                action.Lanes = GetZonesIndexList(accessor);
                tryReadOneLineDetail();
                tryReadOneLineDetail();

                if (accessor.Peek() == KeywordSymbol.ende)
                {
                    action.ShouldEndZone = true;
                    accessor.Next();
                }

                if (accessor.Next() != KeywordSymbol.endOfStream
                    || action.Lanes.Count == 0
                    || (action.DamageCause == KeywordSymbol.invalid && action.DamageType == KeywordSymbol.invalid))
                    return new InvalidAction();

                return action;
            }
            else if (firstSymbol == KeywordSymbol.abbrechen)
                return new CancelAction();
            else
                return new InvalidAction();
        }

    }
}
