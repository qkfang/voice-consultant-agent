using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using VoiceConsultant.FunctionApp.Mcp;
using VoiceConsultant.FunctionApp.Models;
using VoiceConsultant.FunctionApp.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.Configure<CosmosOptions>(builder.Configuration.GetSection("Cosmos"));
builder.Services.Configure<FoundryOptions>(builder.Configuration.GetSection("Foundry"));
builder.Services.Configure<FabricOptions>(builder.Configuration.GetSection("Fabric"));

builder.Services.AddSingleton<CosmosService>();
builder.Services.AddSingleton<FoundryAgentService>();
builder.Services.AddHttpClient<FabricLakehouseService>();
builder.Services.AddSingleton<ConversationInsightService>();

builder.Services.AddSingleton<VoiceConsultantMcpTools>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithTools<VoiceConsultantMcpTools>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
