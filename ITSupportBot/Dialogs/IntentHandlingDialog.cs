//using ITSupportBot.Services;
//using Microsoft.Bot.Builder;
//using Microsoft.Bot.Builder.Dialogs;
//using Microsoft.Bot.Schema;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using System.IO;
//using Newtonsoft.Json.Linq;

//namespace ITSupportBot.Dialogs
//{
//    public class IntentHandlingDialog : ComponentDialog
//    {
//        private readonly AzureOpenAIService _AzureOpenAIService;
//        private readonly ITSupportService _ITSupportService;

//        public IntentHandlingDialog(AzureOpenAIService AzureOpenAIService, ITSupportService ITSupportService) : base(nameof(IntentHandlingDialog))
//        {
//            _AzureOpenAIService = AzureOpenAIService;
//            _ITSupportService = ITSupportService;

//            var waterfallSteps = new WaterfallStep[]
//            {
//                ExtractIntentAndMessageAsync,
//                DisplayMessageAndSuggestionsAsync,
//                HandleSuggestionActionAsync,
//                CollectTicketDetailsAsync
//            };

//            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
//            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
//            InitialDialogId = nameof(WaterfallDialog);
//        }

//        private async Task<DialogTurnResult> ExtractIntentAndMessageAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
//        {
//            string userQuestion = (string)stepContext.Options;

//            string jsonResponse = await _AzureOpenAIService.GetOpenAIResponse(userQuestion);

//            // Parse the JSON response to extract message and suggestions
//            var responseData = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(jsonResponse);

//            stepContext.Values["message"] = responseData.GetProperty("message").GetString();

//            // Convert suggestions to a list of strings for serialization compatibility
//            var suggestions = new List<string>();
//            foreach (var suggestion in responseData.GetProperty("suggestions").EnumerateArray())
//            {
//                suggestions.Add(suggestion.GetString());
//            }
//            stepContext.Values["suggestions"] = suggestions;

//            return await stepContext.NextAsync(null, cancellationToken);
//        }

//        private async Task<DialogTurnResult> DisplayMessageAndSuggestionsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
//        {
//            string message = (string)stepContext.Values["message"];
//            var suggestions = (List<string>)stepContext.Values["suggestions"];

//            List<CardAction> actions = new List<CardAction> { new CardAction(ActionTypes.ImBack, "Edit Message", "Edit Message") };
            

//            foreach (var suggestion in suggestions)
//            {
//                if (suggestion == "UnclearIndent")
//                {
//                    await stepContext.Context.SendActivityAsync("Sorry, the intent was not clear. Let's try again.", cancellationToken: cancellationToken);
//                    return await stepContext.ReplaceDialogAsync(nameof(MainDialog), null, cancellationToken);
//                }

//                else
//                {
//                    actions.Add(new CardAction(ActionTypes.ImBack, suggestion, value: suggestion));
//                }
//            }

//            var reply = MessageFactory.SuggestedActions(actions, message, null, null);
//            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

//            return Dialog.EndOfTurn;
//        }



//        private async Task<DialogTurnResult> HandleSuggestionActionAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
//        {
//            string selectedAction = (string)stepContext.Result; // Get the selected suggestion from the user
//            stepContext.Values["selectedAction"] = selectedAction;

//            // Load the adaptive card JSON from the Cards folder
//            var cardJson = File.ReadAllText("Cards/ticketCreationCard.json");
//            var adaptiveCardAttachment = new Attachment
//            {
//                ContentType = "application/vnd.microsoft.card.adaptive",
//                Content = Newtonsoft.Json.JsonConvert.DeserializeObject(cardJson)
//            };

//            await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);
//            return Dialog.EndOfTurn;
//    }

//        private async Task<DialogTurnResult> CollectTicketDetailsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
//        {
//            // Ensure the result contains the expected data from the Adaptive Card
//            var userInput = stepContext.Context.Activity.Value as JObject;

//            if (userInput != null)
//            {
//                var title = userInput["title"]?.ToString();
//                var description = userInput["description"]?.ToString();

//                // Check if title and description are provided
//                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(description))
//                {
//                    // Save ticket details to the database
//                    await _ITSupportService.SaveTicketAsync(title, description);

//                    await stepContext.Context.SendActivityAsync("Your support ticket has been created successfully.", cancellationToken: cancellationToken);
//                    return await stepContext.NextAsync(null, cancellationToken);
//                }
//                else
//                {
//                    // Prompt the user if fields are empty
//                    await stepContext.Context.SendActivityAsync("Please complete the ticket form.", cancellationToken: cancellationToken);
//                }
//            }
//            else
//            {
//                // In case no data is passed
//                await stepContext.Context.SendActivityAsync("Please complete the ticket form.", cancellationToken: cancellationToken);
//            }

//            return Dialog.EndOfTurn;
//        }





//    }
//}
