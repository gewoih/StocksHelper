using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace StocksHelper.Models
{
	public static class TelegramBot
	{
		public static TelegramBotClient Client { get; set; }

		public static void ConfigureTelegramBot(EventHandler<MessageEventArgs> onMessageHandler)
		{
			Client = new TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotAPI"]);
			Client.StartReceiving();
			Client.OnMessage += onMessageHandler;
		}

		public static async Task SendMessageAsync(ChatId chatId, string answer, IReplyMarkup replyMarkup = null)
		{
			try
			{
				while (answer.Length != 0)
				{
					await Client.SendTextMessageAsync
					(
						chatId: chatId,
						text: answer.Substring(0, Math.Min(answer.Length, 4096)),
						replyMarkup: replyMarkup
					);
					answer = answer.Substring(Math.Min(answer.Length, 4096));
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}
		}
	}
}
