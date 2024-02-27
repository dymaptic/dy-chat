using System.Net;
using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dymaptic.AI.ChatService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[Authorize(AuthenticationSchemes = "ApiKey")]
public class NotSkynet : ControllerBase
{
    private readonly ILogger<NotSkynet> _logger;
    private readonly IConfiguration _configuration;

    public NotSkynet(ILogger<NotSkynet> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }
    
    [HttpPost("{promptName}", Name = "GetResponseAsText")]
    public async Task<string> GetAsText(DyPromptType promptName, DyRequest requests)
    {
        _logger.LogInformation("Starting GetResponseAsText");

        var client = BuildOpenApiClient();
        var opts = BuildChatCompletionsOptions(promptName, requests);

        // Get the response as a string.
        var result = await client.GetChatCompletionsAsync("gpt-4", opts);
        var rspMessage = result.Value.Choices[0].Message.Content;

        return rspMessage;
    }
    

    [HttpPost("{promptName}", Name = "GetResponseAsStream")]
    public async Task GetAsStream(DyPromptType promptName, DyRequest requests)
    {
        _logger.LogInformation("Starting GetResponseAsText");

        var client = BuildOpenApiClient();
        var opts = BuildChatCompletionsOptions(promptName, requests);
        

        this.Response.Headers.Add("Content-Type", "application/json-data-stream");

        await using var sw = new StreamWriter(this.Response.Body);

        Response<StreamingChatCompletions> gptResponse = await client.GetChatCompletionsStreamingAsync("gpt-4", opts);

        using StreamingChatCompletions streamingChatCompletions = gptResponse.Value;

        await foreach (StreamingChatChoice choice in streamingChatCompletions.GetChoicesStreaming())
        {
            await foreach (ChatMessage message in choice.GetMessageStreaming())
            {
                await sw.WriteAsync(message.Content);
                Console.Write(message.Content);
                await sw.FlushAsync();
            }
        }

        await sw.FlushAsync();

    }


    private OpenAIClient BuildOpenApiClient()
    {
        // load openai key
        //var openAiUrl = Environment.GetEnvironmentVariable("OPEN_AI_URL");
        var openAiKey = _configuration["OPEN_AI_KEY"];
        var client = new OpenAIClient(openAiKey);
        return client;
    }

    private ChatCompletionsOptions BuildChatCompletionsOptions(DyPromptType promptName, DyRequest request)
    {
        var requests = request.Messages;
        var context = request.Context;
        
        // load requests
        var messages = requests.Messages
            .Select(i =>
                new ChatMessage(i.Sender == DyChatMessageType.User ? ChatRole.User : ChatRole.Assistant, i.Content))
            .ToList();

        // load prompt
        var prompt = LoadPrompt(promptName, request);

        // TODO: prepare the prompt

        messages.Insert(0, new ChatMessage(ChatRole.System, prompt));

        // load openai client
        var opts = new ChatCompletionsOptions()
        {
            MaxTokens = 4000,
            Messages = { }
        };
        // Note: Messages can't be set in `opts` so this is the best I could come up with (also AddRange doesn't work)
        messages.ForEach(i => opts.Messages.Add(i));
        return opts;
    }


    // load the specified prompt text file from the prompts folder
    private string LoadPrompt(DyPromptType promptName, DyRequest request)
    {
        try
        {
            var promptPath = Path.Combine("prompts", $"{promptName}.txt");

            if (!System.IO.File.Exists(promptPath))
            {
                throw new FileNotFoundException($"Prompt file '{promptName}' not found.");
            }

            var prompt = System.IO.File.ReadAllText(promptPath);
            prompt = prompt.Replace("{currentLayer}", request.Context.CurrentLayer);
            prompt = prompt.Replace("{layers}", JsonSerializer.Serialize(request.Context.Layers));
            return prompt;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading prompt '{promptName}': {ex.Message}");
            return null;
        }
    }

}