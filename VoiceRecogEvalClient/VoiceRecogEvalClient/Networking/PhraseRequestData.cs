namespace VoiceRecogEvalClient
{
    public class PhraseRequestData
    {
        public PhraseRequestData(string phrase, int phraseId)
        {
            Phrase = phrase;
            PhraseId = phraseId;
        }

        public string Phrase { get; set; }
        public int PhraseId { get; set; }
    }
}