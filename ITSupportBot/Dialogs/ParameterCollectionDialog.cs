using ITSupportBot.Services;
using Microsoft.Bot.Builder.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ITSupportBot.Models;
using Microsoft.Bot.Builder;
using System;

public class ParameterCollectionDialog : ComponentDialog
{
    private readonly AzureOpenAIService _AzureOpenAIService;
    private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;

    public ParameterCollectionDialog(AzureOpenAIService AzureOpenAIService, IStatePropertyAccessor<UserProfile> userProfileAccessor)
        : base(nameof(ParameterCollectionDialog))
    {
        _AzureOpenAIService = AzureOpenAIService ?? throw new ArgumentNullException(nameof(AzureOpenAIService));
        _userProfileAccessor = userProfileAccessor ?? throw new ArgumentNullException(nameof(userProfileAccessor));

        var waterfallSteps = new WaterfallStep[]
        {
            PromptForMissingParameterStepAsync,
            HandleParameterResponseStepAsync
        };

        AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
        AddDialog(new TextPrompt(nameof(TextPrompt)));

        InitialDialogId = nameof(WaterfallDialog);
    }

    private async Task<DialogTurnResult> PromptForMissingParameterStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileAccessor.GetAsync(
            stepContext.Context,
            () => new UserProfile(),
            cancellationToken
        );

        if (userProfile == null)
        {
            userProfile = new UserProfile();
            await _userProfileAccessor.SetAsync(stepContext.Context, userProfile, cancellationToken);
        }

        userProfile.ChatHistory ??= new List<ChatTransaction>();

        string userMessage = stepContext.Context.Activity.Text;
        string response = await _AzureOpenAIService.HandleOpenAIResponseAsync(userMessage, userProfile.ChatHistory);

        if (response.Contains("successfully"))
        {
            await stepContext.Context.SendActivityAsync(response, cancellationToken: cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        return await stepContext.PromptAsync(
            nameof(TextPrompt),
            new PromptOptions { Prompt = MessageFactory.Text(response) },
            cancellationToken
        );
    }

    private async Task<DialogTurnResult> HandleParameterResponseStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileAccessor.GetAsync(
            stepContext.Context,
            () => new UserProfile(),
            cancellationToken
        );

        userProfile.ChatHistory ??= new List<ChatTransaction>();

        string userResponse = stepContext.Result?.ToString() ?? string.Empty;
        userProfile.ChatHistory.Add(new ChatTransaction("", userResponse));

        return await stepContext.ReplaceDialogAsync(nameof(ParameterCollectionDialog), null, cancellationToken);
    }
}
