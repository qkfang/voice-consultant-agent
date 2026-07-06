using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace VoiceConsultant.FunctionApp.Agents;

/// <summary>
/// Base class for Foundry agents. Creates a declarative agent version on demand and
/// runs a message through it, auto-approving any MCP tool calls.
/// </summary>
public abstract class BaseAgent
{
    private readonly AIProjectClient _aiProjectClient;
    private readonly string _deploymentName;
    private readonly IList<ResponseTool> _tools;
    private readonly SemaphoreSlim _responseClientLock = new(1, 1);
    private ProjectResponsesClient? _responseClient;

    protected readonly ILogger _logger;
    protected readonly string _agentId;

    public string AgentId => _agentId;
    public string Instructions { get; }

    protected BaseAgent(
        AIProjectClient aiProjectClient,
        string agentId,
        string deploymentName,
        string instructions,
        IList<ResponseTool>? tools = null,
        ILogger? logger = null)
    {
        _aiProjectClient = aiProjectClient;
        _deploymentName = deploymentName;
        _agentId = agentId;
        Instructions = instructions;
        _tools = tools ?? [];
        _logger = logger ?? LoggerFactory.Create(b => b.AddConsole()).CreateLogger(agentId);
    }

    public async Task<string> RunAsync(string message)
    {
        var responseClient = await EnsureResponseClientAsync();

        CreateResponseOptions? nextOptions = new()
        {
            InputItems = { ResponseItem.CreateUserMessageItem(message) }
        };

        ResponseResult? result = null;

        while (nextOptions is not null)
        {
            result = await responseClient.CreateResponseAsync(nextOptions);
            nextOptions = null;

            foreach (var item in result.OutputItems)
            {
                if (item is McpToolCallApprovalRequestItem mcpCall)
                {
                    nextOptions ??= new CreateResponseOptions { PreviousResponseId = result.Id };
                    nextOptions.InputItems.Add(ResponseItem.CreateMcpApprovalResponseItem(mcpCall.Id, approved: true));
                }
            }
        }

        return result?.GetOutputText() ?? string.Empty;
    }

    private async Task<ProjectResponsesClient> EnsureResponseClientAsync()
    {
        if (_responseClient is not null)
        {
            return _responseClient;
        }

        await _responseClientLock.WaitAsync();
        try
        {
            if (_responseClient is not null)
            {
                return _responseClient;
            }

            var agentDefinition = new DeclarativeAgentDefinition(model: _deploymentName)
            {
                Instructions = Instructions
            };

            foreach (var tool in _tools)
            {
                agentDefinition.Tools.Add(tool);
            }

            var agentVersion = await _aiProjectClient.AgentAdministrationClient.CreateAgentVersionAsync(
                _agentId,
                new ProjectsAgentVersionCreationOptions(agentDefinition));

            _responseClient = _aiProjectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agentVersion.Value.Name);
            return _responseClient;
        }
        finally
        {
            _responseClientLock.Release();
        }
    }
}
