using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Threading;
using System.Threading.Tasks;

namespace ITSupportBot.Dialogs
{
    public class NumberDialog : ComponentDialog
    {
        public NumberDialog()
            : base(nameof(NumberDialog))
        {
            var waterfallSteps = new WaterfallStep[]
            {
                NumberStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new NumberPrompt<long>(nameof(NumberPrompt<long>), MobileNumberValidation));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> NumberStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter your Number."),
                RetryPrompt = MessageFactory.Text("Please enter a valid number."),
            };

            return await stepContext.PromptAsync(nameof(NumberPrompt<long>), promptOptions, cancellationToken);
        }

        private async Task<bool> MobileNumberValidation(PromptValidatorContext<long> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                await promptContext.Context.SendActivityAsync("Please enter a valid mobile number.", cancellationToken: cancellationToken);
                return false;
            }

            string input = promptContext.Recognized.Value.ToString();
            if (input.Length != 10)
            {
                await promptContext.Context.SendActivityAsync("The mobile number should be 10 digits.", cancellationToken: cancellationToken);
                return false;
            }

            return true;
        }
    }
}
