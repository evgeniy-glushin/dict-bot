using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using static Domain;
using static Core.Bot;
//using Telegram.Bot.Types.ReplyMarkups;
//using Telegram.Bot.Types.InlineKeyboardButtons;
//using Telegram.Bot.Types;

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

            //var keyboard = new ReplyKeyboardMarkup(new[]
            //     {
            //        new [] // first row
            //        {
            //            new KeyboardButton("1.1"),
            //            new KeyboardButton("1.2"),
            //        },
            //        new [] // last row
            //        {
            //            new KeyboardButton("2.1"),
            //            new KeyboardButton("2.2"),
            //        }
            //    });
            
            // return our reply to the user
            var payload = new BotPayload(activity.From.Id, activity.From.Name, activity.Text);
            await context.PostAsync("hi");

            context.Wait(MessageReceivedAsync);
        }
    }
}