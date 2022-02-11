using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using VoiceRecogEvalServer.FieldMAppPhraseRecognition;
using VoiceRecogEvalServer.Models;

namespace VoiceRecogEvalServer
{
    public class Startup
    {
        PhraseRequestSelectionParameters PhraseRequestSelectionParameters;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            PhraseRequestSelectionParameters = new PhraseRequestSelectionParameters
            {
                LSHMaxBucketSize = Configuration.GetValue<int>("Preselection:LSHMaxBucketSize"),
                PhrasePairMinHeapSize = Configuration.GetValue<int>("Preselection:PhrasePairMinHeapSize"),
                PhrasesSetSize = Configuration.GetValue<int>("Preselection:PhrasesSetSize"),
                ShinglePartCount = Configuration.GetValue<int>("Preselection:ShinglePartCount"),
                MinHashSignatureSize = Configuration.GetValue<int>("Preselection:MinHashSignatureSize"),
                LSHSimilarityThreshold = Configuration.GetValue<double>("Preselection:LSHSimilarityThreshold"),
                LSHAllowedFalseNegativeRate = Configuration.GetValue<double>("Preselection:LSHAllowedFalseNegativeRate")
            };

            if (PhraseRequestSelectionParameters.LSHMaxBucketSize <= 0)
                throw new ArgumentException("LSHMaxBucketSize must be greater than 0");
            if (PhraseRequestSelectionParameters.PhrasePairMinHeapSize <= 0)
                throw new ArgumentException("PhrasePairMinHeapSize must be greater than 0");
            if (PhraseRequestSelectionParameters.PhrasesSetSize <= 0)
                throw new ArgumentException("PhrasesSetSize must be greater than 0");
            if (PhraseRequestSelectionParameters.ShinglePartCount <= 0 || PhraseRequestSelectionParameters.ShinglePartCount > 4)
                throw new ArgumentException("ShinglePartCount must be in range 1-4");
            if (PhraseRequestSelectionParameters.LSHSimilarityThreshold < 0 || PhraseRequestSelectionParameters.LSHSimilarityThreshold > 1)
                throw new ArgumentException("LSHSimilarityThreshold must be between 0 and 1");
            if (PhraseRequestSelectionParameters.LSHAllowedFalseNegativeRate < 0 || PhraseRequestSelectionParameters.LSHAllowedFalseNegativeRate > 1)
                throw new ArgumentException("LSHAllowedFalseNegativeRate must be between 0 and 1");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IPhraseRecognizer<KeywordSymbol> phraseRecognizer = new FieldMAppPhraseRecognizer();
            services.AddSingleton<IPhraseRecognizer>(phraseRecognizer);
            services.AddSingleton<IPhraseRequestSelector>(new PhraseRequestSelector<KeywordSymbol>(phraseRecognizer, PhraseRequestSelectionParameters));
            services.AddDbContext<RecordingDatabaseContext>();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
