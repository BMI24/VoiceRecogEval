using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.FieldMAppPhraseRecognition
{
    public enum KeywordSymbol
    {
        invalid,
        anfang, ende, abbrechen,
        gering, mittel, hoch,
        hang, nass, maus, wild, lehm, trocken, sand, kuppe, ton, verdichtung, wende, waldrand,
        spur, zone,
        number0, number1, number2, number3, number4, number5, number6,
        unk, endOfStream
    }
}
