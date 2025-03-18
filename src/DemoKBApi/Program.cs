using DemoKBApi.BL;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Reflection;

namespace DemoKBApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var configuration = builder.Configuration;

            string modelId = configuration["AOAI_MODEL_ID"];
            string aoai_Endpoint = configuration["AOAI_ENDPOINT"];
            string aoai_apikey = configuration["AOAI_APIKEY"];

            var settings = Settings.GetSettings();
            settings.AISearchEndpoint = configuration["AI_SEARCH_ENDPOINT"];
            settings.AISearchApiKey = configuration["AI_SEARCH_APIKEY"];
            settings.AISearchIndex = configuration["AI_SEARCH_INDEX"];

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //Sematic Kernal
            builder.Services.AddSingleton<Kernel>(sp =>
            {
                var kerenalBuilder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(modelId, aoai_Endpoint, aoai_apikey);

                //kerenalBuilder.Plugins.AddFromType<LightsPlugin>();
                kerenalBuilder.Plugins.AddFromType<EmpInsurancePlugin>();
                return kerenalBuilder.Build();

            }
             );

            builder.Services.AddSingleton<ChatHistoryService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.UseCors(x => x
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true) // allow any origin
                    );

            app.Run();
        }
    }
}
