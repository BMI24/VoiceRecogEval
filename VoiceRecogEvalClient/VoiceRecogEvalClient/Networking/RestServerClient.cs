using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceRecogEvalClient.Networking
{
    class RestServerClient : IServerClient
    {
        Uri ServerUri { get; }
        Uri RestVoiceLineCountTargetUri { get; }
        Uri RestVoiceLineCountUri { get; }
        Uri RestRequestedVoiceLineUri { get; }
        Uri RestPostRecordedAudioUri { get; }

        string Username { get; }
        public RestServerClient(string serverUri, string username)
        {
            Username = username;
            ServerUri = new Uri(serverUri);
            RestVoiceLineCountTargetUri = new Uri(ServerUri, "api/RecordingUpload/target");
            RestVoiceLineCountUri = new Uri(ServerUri, "api/RecordingUpload/count/" + username);
            RestRequestedVoiceLineUri = new Uri(ServerUri, "api/RecordingUpload/req/" + username);
            RestPostRecordedAudioUri = new Uri(ServerUri, "api/RecordingUpload/");
    }

        public async Task<int?> GetVoiceLineCountTarget(CancellationToken cancellationToken)
        {
            var restClient = new RestClient();
            var targetCountRequest = new RestRequest(RestVoiceLineCountTargetUri);
            var targetCountResponse = await restClient.ExecuteGetAsync(targetCountRequest);
            if (!targetCountResponse.IsSuccessful)
                return null;
            var targetVoiceCount = int.Parse(targetCountResponse.Content);
            if (cancellationToken.IsCancellationRequested)
                return null;
            return targetVoiceCount;
        }

        public async Task<int?> GetVoiceLineCountCurrent(CancellationToken cancellationToken)
        {
            var restClient = new RestClient();
            var currentCountRequest = new RestRequest(RestVoiceLineCountUri);
            var currentCountResponse = await restClient.ExecuteGetAsync(currentCountRequest);
            if (!currentCountResponse.IsSuccessful)
                return null;
            var currentVoiceCount = int.Parse(currentCountResponse.Content);
            if (cancellationToken.IsCancellationRequested)
                return null;
            return currentVoiceCount;
        }

        public async Task<PhraseRequestData> GetRequestedPhrase(CancellationToken cancellationToken)
        {
            var restClient = new RestClient();
            var request = new RestRequest(RestRequestedVoiceLineUri);
            var response = await restClient.ExecuteGetAsync(request, cancellationToken);
            if (!response.IsSuccessful)
                return null;

            var requestedPhraseData = JsonConvert.DeserializeObject<PhraseRequestData>(response.Content);
            return requestedPhraseData;
        }

        public async Task<bool> UploadPhrase(PhraseRequestData requestedPhraseData, string audioFilePath, CancellationToken cancellationToken)
        {
            var uploadRequest = new RestRequest(RestPostRecordedAudioUri);
            uploadRequest.AddFile("File", audioFilePath);
            uploadRequest.AddParameter("PhraseId", requestedPhraseData.PhraseId);
            uploadRequest.AddParameter("Username", Username);
            uploadRequest.AddParameter("RequestedVoiceLine", requestedPhraseData.Phrase);

            var restClient = new RestClient();

            var response = await restClient.ExecutePostAsync(uploadRequest, cancellationToken);
            return !cancellationToken.IsCancellationRequested && response.IsSuccessful;
        }
    }
}
