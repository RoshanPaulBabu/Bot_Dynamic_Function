using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITSupportBot.Models; // Assuming the updated ChatTransaction class is here

namespace ITSupportBot.Services
{
    public class AzureOpenAIService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly ITSupportService _ITSupportService;

        public AzureOpenAIService(IConfiguration configuration, ILogger<AzureOpenAIService> logger, ITSupportService ITSupportService)
        {
            _configuration = configuration;
            _logger = logger;
            _ITSupportService = ITSupportService;
        }

        public async Task<string> HandleOpenAIResponseAsync(string userQuestion, List<ChatTransaction> chatHistory)
        {
            try
            {
                // Initialize OpenAI client
                var apiKeyCredential = new System.ClientModel.ApiKeyCredential(_configuration["AzureOpenAIKey"]);
                var client = new AzureOpenAIClient(new Uri(_configuration["AzureOpenAIEndpoint"]), apiKeyCredential);
                var chatClient = client.GetChatClient("gpt-35-turbo-16k");

                // Define JSON schema for support ticket creation
                string jsonSchemaTicket = @"
                {
                    ""type"": ""object"",
                    ""properties"": {
                        ""title"": { ""type"": ""string"", ""description"": ""Title of the issue."" },
                        ""description"": { ""type"": ""string"", ""description"": ""One-line description of the issue."" }
                    },
                    ""required"": [""title"", ""description""]
                }";

                string jsonSchemaWeather = @"
                {
                    ""type"": ""object"",
                    ""properties"": {
                        ""latitude"": { ""type"": ""number"", ""description"": ""Latitude of the location."" },
                        ""longitude"": { ""type"": ""number"", ""description"": ""Longitude of the location."" },
                        ""locationName"": { ""type"": ""string"", ""description"": ""Optional name of the location."" }
                    },
                    ""required"": [""latitude"", ""longitude""]
                }";

                // Define function tool parameters
                var ticketFunctionParameters = BinaryData.FromString(jsonSchemaTicket);

                var weatherFunctionParameters = BinaryData.FromString(jsonSchemaWeather);

                // Define the function tool
                var createSupportTicketTool = ChatTool.CreateFunctionTool(
                    "createSupportTicket",
                    "Creates a new support ticket based on user input.",
                    ticketFunctionParameters
                );
                var weathertool= ChatTool.CreateFunctionTool(
                    "collectLocationDetails",
                    "Collects latitude and longitude from the user for weather information.",
                    weatherFunctionParameters
                );

                var chatOptions = new ChatCompletionOptions
                {
                    Tools = { createSupportTicketTool, weathertool }
                };

                // Prepare the chat history
                var chatMessages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are an intelligent assistant that helps create support tickets or collect information for getting weather conditions.")
                };




                // Add previous conversation history to chat messages
                foreach (var transaction in chatHistory)
                {
                    if (!string.IsNullOrEmpty(transaction.UserMessage))
                        chatMessages.Add(new UserChatMessage(transaction.UserMessage));
                    if (!string.IsNullOrEmpty(transaction.BotMessage))
                        chatMessages.Add(new AssistantChatMessage(transaction.BotMessage));
                }

                // Add the current user question
                chatMessages.Add(new UserChatMessage(userQuestion));


                // Perform chat completion
                ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages.ToArray(), chatOptions);

                if (completion.FinishReason == ChatFinishReason.ToolCalls)
                {
                    foreach (var toolCall in completion.ToolCalls)
                    {
                        if (toolCall.FunctionName == "createSupportTicket")
                        {
                            // Parse tool call arguments
                            var inputData = toolCall.FunctionArguments.ToObjectFromJson<Dictionary<string, string>>();
                            _logger.LogInformation($"Title: {inputData.GetValueOrDefault("title")}, Description: {inputData.GetValueOrDefault("description")}");

                            // Extract required parameters
                            string title = inputData.GetValueOrDefault("title");
                            string description = inputData.GetValueOrDefault("description");

                            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
                            {
                                // Save the assistant's message for continuity
                                chatHistory.Add(new ChatTransaction(completion.Content[0]?.Text ?? "Please provide more details.", userQuestion));
                                return completion.Content[0]?.Text ?? "Please provide more details.";
                            }

                            // All required parameters collected
                            await _ITSupportService.SaveTicketAsync(title, description);

                            // Update chat history
                            chatHistory.Add(new ChatTransaction("Your support ticket has been created successfully!", userQuestion));
                            return "Your support ticket has been created successfully!";
                        }
                        else if (toolCall.FunctionName == "collectLocationDetails")
                        {
                            var inputData = toolCall.FunctionArguments.ToObjectFromJson<Dictionary<string, object>>();

                            if (inputData.TryGetValue("latitude", out var latitudeObj) &&
                                inputData.TryGetValue("longitude", out var longitudeObj))
                            {
                                // Convert latitude and longitude to strings or handle them as numbers
                                var latitude = latitudeObj.ToString();
                                var longitude = longitudeObj.ToString();

                                return $"Collected location details: Latitude = {latitude}, Longitude = {longitude}.";
                            }

                            return "Please provide both latitude and longitude for location details.";
                        }

                    }
                }

                // Save the assistant's response for continuity
                string response = completion.Content[0]?.Text ?? "I'm unable to process your request at this time.";
                chatHistory.Add(new ChatTransaction(response, userQuestion));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in HandleOpenAIResponseAsync: {ex.Message}");
                return "An error occurred while processing your request. Please try again.";
            }
        }
    }
}
