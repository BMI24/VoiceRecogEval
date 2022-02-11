using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.Models
{
    public class VoiceRecording
    {
        [Key]
        public int RecordingId { get; set; }
        public string Username { get; set; }
        public byte[] SoundFile { get; set; }
        public string RequestedVoiceLine { get; set; }
        public DateTime UploadedUtcTime { get; set; }
        public bool HasUserError { get; set; }
    }
}
