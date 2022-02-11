using System.Threading;
using System.Threading.Tasks;

namespace VoiceRecogEvalClient.Networking
{
    internal interface IServerClient
    {
        Task<int?> GetVoiceLineCountTarget(CancellationToken cancellationToken);
        Task<int?> GetVoiceLineCountCurrent(CancellationToken cancellationToken);
        Task<PhraseRequestData> GetRequestedPhrase(CancellationToken cancellationToken);
        Task<bool> UploadPhrase(PhraseRequestData requestedPhraseData, string audioFilePath, CancellationToken cancellationToken);
    }
}