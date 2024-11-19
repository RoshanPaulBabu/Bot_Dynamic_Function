using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ITSupportBot.Services;
using ITSupportBot.Models;

namespace ITSupportBot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly AzureOpenAIService _AzureOpenAIService;
        private readonly ITSupportService _ITSupportService;

        public MainDialog(UserState userState, AzureOpenAIService AzureOpenAIService, ITSupportService ITSupportService)
            : base(nameof(MainDialog))
        {
            _userProfileAccessor = userState.CreateProperty<UserProfile>("UserProfile");
            _AzureOpenAIService = AzureOpenAIService;
            _ITSupportService = ITSupportService;

            var waterfallSteps = new WaterfallStep[]
            {
                NameStepAsync,
                CallNumberDialogAsync,
                AskHelpQueryStepAsync,
                BeginParameterCollectionStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new NumberDialog());
            AddDialog(new TextPrompt("HelpQueryPrompt"));
            AddDialog(new ParameterCollectionDialog(_AzureOpenAIService, _userProfileAccessor));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(
                nameof(TextPrompt),
                new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") },
                cancellationToken
            );
        }

        private async Task<DialogTurnResult> CallNumberDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save the name to state
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            userProfile.Name = (string)stepContext.Result;

            // Call the NumberDialog to prompt for the user's number
            return await stepContext.BeginDialogAsync(nameof(NumberDialog), null, cancellationToken);
        }

        private async Task<DialogTurnResult> AskHelpQueryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Save the number to the user profile
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            userProfile.Number = (long)stepContext.Result;

            // Prompt the user for their issue
            return await stepContext.PromptAsync(
                "HelpQueryPrompt",
                new PromptOptions { Prompt = MessageFactory.Text("Hello! How can I help you today?") },
                cancellationToken
            );
        }

        private async Task<DialogTurnResult> BeginParameterCollectionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the user query
            string userMessage = (string)stepContext.Result;

            // Get user profile and initialize chat history if necessary
            var userProfile = await _userProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);
            userProfile.ChatHistory ??= new List<ChatTransaction>();
            string response = await _AzureOpenAIService.HandleOpenAIResponseAsync(userMessage, userProfile.ChatHistory);



            // Check if the process is complete (all parameters collected)
            if (response.Contains("successfully")) // Success message from AzureOpenAIService
            {
                await stepContext.Context.SendActivityAsync(response, cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            // Otherwise, begin the ParameterCollectionDialog to collect missing information
            return await stepContext.BeginDialogAsync(nameof(ParameterCollectionDialog), null, cancellationToken);
        }
    }
}
