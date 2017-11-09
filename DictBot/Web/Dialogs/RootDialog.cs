using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using static BotModels;
using static Core.Bot;

namespace Web.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            // return our reply to the user

            var payload = new TranslatePayload(activity.From.Id, activity.From.Name, activity.Text);
            await context.PostAsync(await respond(payload));

            context.Wait(MessageReceivedAsync);
        }
    }
}