using Azure.AI.OpenAI.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel;
using Azure;
using System.Text.Json;
using System.Text;
using System.Reflection;

namespace DemoKBApi.BL
{
    public class SKOrchestrator
    {
        string aisearchendpoint = Settings.GetSettings().AISearchEndpoint;
        string aisearchkey = Settings.GetSettings().AISearchApiKey;
        string aisearchindex = Settings.GetSettings().AISearchIndex;

        public ChatHistory History { get; private set; }
        private string _sessionId;
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;

        public SKOrchestrator(ChatHistoryService chatHistory, string sessionId, Kernel kernel)
        {
            //chatHistory.Clear();

            this._sessionId = sessionId;
            this.History = chatHistory.GetOrCreateHistory(sessionId);
            this._kernel = kernel;
            this._chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            if (this.History.Count == 0)
            {
                //change file refernce to relative
                string promptFile = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "SystemMessage.txt");
                var systemPrompt = File.ReadAllText(promptFile);

                this.History.AddSystemMessage(systemPrompt);
            }
        }

        public async Task<ChatMessageContent> Query(string userInput)
        {
            ChatMessageContent result = await QueryFunctions(userInput);

            if (result == null)
            {
                result = await QuerySearch(userInput);
            }
            else if (result == null)
            {
                result = new ChatMessageContent(AuthorRole.Assistant, "no function selected");
            }


            return result;
        }

        private async Task<ChatMessageContent> QueryFunctions(string userInput)
        {
            History.AddUserMessage(userInput);

            AzureOpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false)
            };

            ChatMessageContent result;

            // Get the response from the AI
            result = await _chatCompletionService.GetChatMessageContentAsync(
                History,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);

            // Check if the AI model has chosen any function for invocation.
            IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(result);
            if (!functionCalls.Any())
            {
                return null; //no function selected
            }

            while (true)
            {
                if (result.Content is not null)
                {
                    // Print the results
                    Console.WriteLine("Assistant > " + result);

                    // Add the message from the agent to the chat history
                    string content = result.Content.Replace("[spoken]","") ?? string.Empty;
                    History.AddMessage(result.Role,content);

                    return result;

                    //break;
                }

                History.Add(result);

                // Check if the AI model has chosen any function for invocation.
                functionCalls = FunctionCallContent.GetFunctionCalls(result);
                if (!functionCalls.Any())
                {
                    break;
                }

                // Sequentially iterating over each chosen function, invoke it, and add the result to the chat history.
                foreach (FunctionCallContent functionCall in functionCalls)
                {
                    try
                    {
                        // Invoking the function
                        FunctionResultContent resultContent = await functionCall.InvokeAsync(_kernel);

                        // Adding the function result to the chat history
                        History.Add(resultContent.ToChatMessage());
                    }
                    catch (Exception ex)
                    {
                        // Adding function exception to the chat history.
                        History.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                        // or
                        //chatHistory.Add(new FunctionResultContent(functionCall, "Error details that the AI model can reason about.").ToChatMessage());
                    }
                }

                // Get the response from the AI
                result = await _chatCompletionService.GetChatMessageContentAsync(
                History,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);
            }

            return null;
        }

        private async Task<ChatMessageContent> QuerySearch(string userInput)
        {
            History.AddUserMessage(userInput);

            AzureOpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None()
            };

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            //openAIPromptExecutionSettings.AzureChatDataSource = new AzureSearchChatDataSource()
            AzureSearchChatDataSource aschs = new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(aisearchendpoint),
                IndexName = aisearchindex,
                Authentication = DataSourceAuthentication.FromApiKey(aisearchkey)
            };

            openAIPromptExecutionSettings.AzureChatDataSource = aschs;
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Get the response from the AI
            var result = await _chatCompletionService.GetChatMessageContentAsync(
            History,
                executionSettings: openAIPromptExecutionSettings,
                kernel: _kernel);

            History.AddAssistantMessage(result.Content ?? string.Empty);

            return result;
        }


        #region "Streaming methods"

        public async Task<SKResultOutput> QueryStream(string userInput, HttpResponse Response)
        {
            SKResultOutput result;
            History.AddUserMessage(userInput);

            result = await QueryFunctionsStream(userInput, Response);

            if (!result.ExecutedFunction)
            {
                result = await QuerySearchStream(userInput, Response);
            }
            //else if (result == null)
            //{
            //    //result = new ChatMessageContent(AuthorRole.Assistant, "no function selected");
            //}

            return result;

        }

        private async Task<SKResultOutput> QueryFunctionsStream(string userInput, HttpResponse Response)
        {
            //History.AddUserMessage(userInput);
            SKResultOutput skResult = new SKResultOutput();

            AzureOpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false)
            };

            ChatMessageContent result;

            // Get the response from the AI
            result = await _chatCompletionService.GetChatMessageContentAsync(
                History,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);

            // Check if the AI model has chosen any function for invocation.
            IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(result);
            if (!functionCalls.Any())
            {
                skResult.ExecutedFunction = false; //no function selected
                return skResult;
            }

            while (true)
            {
                if (result.Content is not null)
                {
                    // Print the results
                    //Console.WriteLine("Assistant > " + result);

                    // Add the message from the agent to the chat history
                    ChatResponse chatResponse = new ChatResponse();
                    string[] responses = result.Content.Split(new string[] { "[not spoken]" }, StringSplitOptions.None);

                    string contentSpoken = responses[0].Replace("[spoken]", "") ?? string.Empty;
                    string contentNotSpoken = string.Empty;

                    if(contentNotSpoken.Length > 0)
                    {
                        contentNotSpoken = responses[1].Replace("[not spoken]", "") ?? string.Empty;
                    }
                    
                    History.AddMessage(result.Role, result.Content ?? string.Empty);
                    
                    if (result != null)
                    {
                        chatResponse.Role = result.Role.ToString();
                        chatResponse.Content = contentSpoken; // result.Content;
                        chatResponse.NotSpokenContent = contentNotSpoken;

                        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chatResponse)}\n\n");
                        await Response.Body.FlushAsync();
                    }

                    skResult.ExecutedFunction = true; //function executed
                    skResult.Output = result.Content;
                    return skResult;

                    //break;
                }

                History.Add(result);

                // Check if the AI model has chosen any function for invocation.
                functionCalls = FunctionCallContent.GetFunctionCalls(result);
                if (!functionCalls.Any())
                {
                    break;
                }

                // Sequentially iterating over each chosen function, invoke it, and add the result to the chat history.
                foreach (FunctionCallContent functionCall in functionCalls)
                {
                    try
                    {
                        // Invoking the function
                        FunctionResultContent resultContent = await functionCall.InvokeAsync(_kernel);

                        // Adding the function result to the chat history
                        History.Add(resultContent.ToChatMessage());
                    }
                    catch (Exception ex)
                    {
                        // Adding function exception to the chat history.
                        History.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                        // or
                        //chatHistory.Add(new FunctionResultContent(functionCall, "Error details that the AI model can reason about.").ToChatMessage());
                    }
                }

                // Get the response from the AI
                result = await _chatCompletionService.GetChatMessageContentAsync(
                History,
                    executionSettings: openAIPromptExecutionSettings,
                    kernel: _kernel);
            }

            skResult.ExecutedFunction = false; //no function selected
            return skResult;
        }

        private async Task<SKResultOutput> QuerySearchStream(string userInput, HttpResponse Response)
        {
            //History.AddUserMessage(userInput);
            SKResultOutput sKResult = new SKResultOutput();

            AzureOpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.None()
            };

#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            //openAIPromptExecutionSettings.AzureChatDataSource = new AzureSearchChatDataSource()
            AzureSearchChatDataSource aschs = new AzureSearchChatDataSource()
            {
                Endpoint = new Uri(aisearchendpoint),
                IndexName = aisearchindex,
                Authentication = DataSourceAuthentication.FromApiKey(aisearchkey)
            };

            openAIPromptExecutionSettings.AzureChatDataSource = aschs;
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            //Get the response from the AI
            StringBuilder sb = new StringBuilder();
            StringBuilder sbNotSpokenText = new StringBuilder();

            int responseType = 0; //0: no response, 1: spoken, 2: not spoken

            await foreach (var chatUpdate in _chatCompletionService.GetStreamingChatMessageContentsAsync(History,
                executionSettings: openAIPromptExecutionSettings, kernel: _kernel))
            {
                ChatResponse chatResponse = new ChatResponse();

                //set response type
                if (!string.IsNullOrEmpty(chatUpdate.Content) && chatUpdate.Content.Trim() == "[spoken]")
                {
                    responseType = 1;
                    continue;
                }
                else if (!string.IsNullOrEmpty(chatUpdate.Content) && chatUpdate.Content.Trim() == "[not spoken]")
                {
                    responseType = 2;
                    continue;
                }

                if (chatUpdate.Role.HasValue || !string.IsNullOrEmpty(chatUpdate.Content))
                {
                    if (chatUpdate.Role.HasValue)
                    {
                        chatResponse.Role = chatUpdate.Role.Value.ToString();
                    }
                    if (!string.IsNullOrEmpty(chatUpdate.Content))
                    {
                        if (responseType == 1)
                            chatResponse.Content = chatUpdate.Content;
                        else if (responseType == 2)
                            sbNotSpokenText.Append(chatUpdate.Content);

                        sb.Append(chatUpdate.Content);
                    }

                    if (responseType != 2) //if not [not spoken text]
                    {
                        await Response.WriteAsync($"data: {JsonSerializer.Serialize(chatResponse)}\n\n");
                        await Response.Body.FlushAsync();
                    }

                }
            }

            History.AddAssistantMessage(sb.ToString());

            //write not spoken text at the end
            if (sbNotSpokenText.Length > 0)
            {
                ChatResponse chatResponse = new ChatResponse();
                chatResponse.NotSpokenContent = sbNotSpokenText.ToString();
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(chatResponse)}\n\n");
                await Response.Body.FlushAsync();
            }

            sKResult.ExecutedFunction = false; //no function selected
            sKResult.Output = sb.ToString();

            return sKResult;
        }

        #endregion
    }

    public class SKResultOutput
    {
        public bool ExecutedFunction { get; set; }
        public string Output { get; set; }
    }
}
