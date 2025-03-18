using DemoKBApi.BL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.Cors;

namespace DemoKBApi.Controllers
{
    //[DisableCors]
    [ApiController]
    [Route("[controller]/[action]")]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ChatHistoryService _chatHistoryService;

        public ChatController(Kernel kerenal, ChatHistoryService chatHistoryService, ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _kernel = kerenal;
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            _chatHistoryService = chatHistoryService;
        }

        [HttpPost]
        public async Task<IActionResult> Post(string input)
        {

            // Use a fixed session ID for simplicity, or generate a unique one per user/session
            var sessionId = "default-session";
            var history = _chatHistoryService.GetOrCreateHistory(sessionId);
            // Add user input
            history.AddUserMessage(input);
            var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };
            // Get the response from the AI
            var result = await _chatCompletionService.GetChatMessageContentAsync(history,
                executionSettings: openAIPromptExecutionSettings, kernel: _kernel);

            // Add the message from the agent to the chat history
            history.AddAssistantMessage(result.Content ?? string.Empty);
            return new JsonResult(new { reply = result.Content });
        }

        //input: Please toggle the light
        [HttpPost]
        public async Task PostStream(UserInput input)
        {

            // Use a fixed session ID for simplicity, or generate a unique one per user/session
            var sessionId = "default-session";
            var history = _chatHistoryService.GetOrCreateHistory(sessionId);
            // Add user input
            history.AddUserMessage(input.Query);
            var openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            };
            // Get the response from the AI
            //var result = await _chatCompletionService.GetChatMessageContentAsync(history,
            //    executionSettings: openAIPromptExecutionSettings, kernel: _kernel);

            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            await foreach (var chatUpdate in _chatCompletionService.GetStreamingChatMessageContentsAsync(history, executionSettings: openAIPromptExecutionSettings, kernel: _kernel))
            {
                // Add the message from the agent to the chat history
                //history.AddAssistantMessage(chatUpdate.Content ?? string.Empty);
                //Console.WriteLine(chatUpdate.Content);

                ChatResponse chatResponse = new ChatResponse();

                if (chatUpdate.Role.HasValue || !string.IsNullOrEmpty(chatUpdate.Content))
                {
                    if (chatUpdate.Role.HasValue)
                    {
                        chatResponse.Role = chatUpdate.Role.Value.ToString();
                        //await Response.WriteAsync($"data: {chatUpdate.Role.Value.ToString().ToUpperInvariant()}: \n\n");
                    }
                    if (!string.IsNullOrEmpty(chatUpdate.Content))
                    {
                        chatResponse.Content = chatUpdate.Content;
                        //await Response.WriteAsync($"data: {chatUpdate.Content}\n\n");
                    }

                    await Response.WriteAsync($"data: {JsonSerializer.Serialize(chatResponse)}\n\n");
                    await Response.Body.FlushAsync();
                }

            }

            await Response.WriteAsync($"data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
    }

    public class UserInput
    {
        public string Query { get; set; }
    }

    public class ChatResponse
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }
}
