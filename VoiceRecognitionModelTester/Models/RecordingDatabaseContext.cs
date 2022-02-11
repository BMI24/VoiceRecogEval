using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VoiceRecogEvalServer.Models
{
    public class RecordingDatabaseContext : DbContext
    {
        public const string DatabaseFileName = "recordings.db";

        public RecordingDatabaseContext()
        {

        }
        public RecordingDatabaseContext(DbContextOptions<RecordingDatabaseContext> options) : base(options)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = DatabaseFileName };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);

            optionsBuilder.UseSqlite(connection);
        }

        public DbSet<VoiceRecording> Recordings { get; set; }
    }
}
