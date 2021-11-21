using Newtonsoft.Json;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories;
using StocksHelper.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{
			TelegramBot.ConfigureTelegramBot(this.OnMessageHandler);
			//Загрузка логов
			this.LogRecords = new ObservableCollection<LogRecord>(new LogRecordsRepository(new BaseDataContext()).GetAll());
			this._StopWatcher = new Stopwatch();

			DispatcherTimer mainTimer = new DispatcherTimer() { Interval = new TimeSpan(1, 0, 0) };
			mainTimer.Tick += this.MainTimer_Tick;
			mainTimer.Start();
		}
		#endregion

		#region Properties
		//Коллекция логов с привязкой к DataGridView
		private ObservableCollection<LogRecord> _LogRecords;
		public ObservableCollection<LogRecord> LogRecords
		{
			get => _LogRecords;
			set => Set(ref _LogRecords, value);
		}

		private Stopwatch _StopWatcher;
		#endregion

		#region Methods
		private void MainTimer_Tick(object sender, object e)
		{
			DateTime currentDateTime = DateTime.Now;
			if (currentDateTime.DayOfWeek != DayOfWeek.Saturday &&
				currentDateTime.DayOfWeek != DayOfWeek.Sunday &&
				currentDateTime.Hour == 6)
			{
				this._StopWatcher = Stopwatch.StartNew();
				this.LoadMissingQuotes();
				this._StopWatcher.Stop();
			}
		}

		private async void OnMessageHandler(object sender, MessageEventArgs e)
		{
			//Создаем Stopwatch для замера скорости обработки запроса
			var watcher = Stopwatch.StartNew();

			var msg = e.Message;
			//Итоговые переменные, по которым будем формировать ответ
			Telegram.Bot.Types.ChatId answerChatId = msg.Chat.Id;
			string answerMessage = "Выберите команду:";
			IReplyMarkup answerReplyMarkup = GetMainMenuButtons();
			//Ищем пользователя по telegramId
			User user = new UsersRepository(new BaseDataContext()).GetAll().FirstOrDefault(u => u.TelegramId == msg.From.Id.ToString());

			//Зарегистрировать пользователя, если он не найден и отправить ему приветственное сообщение
			if (user == null)
			{
				user = this.RegisterUser(msg.From.Username, msg.From.Id.ToString());

				answerMessage = $"Добро пожаловать! Я - ваш новый личный помощник на фондовом рынке. Все, что вам нужно - это сформировать список " +
					"интересующих вас акций через интерфейс ниже. После добавления акций, я буду присылать вам ежедневные отчеты и сигналы по выбранным компаниям.";
			}

			if (msg != null)
			{
				switch (msg.Text)
				{
					case "Добавить акцию" or "Удалить акцию":
						if (msg.Text == "Добавить акцию")
							answerMessage = "Введите тикер акции для добавления:";
						else
							answerMessage = "Введите тикер акции для удаления:";
						answerReplyMarkup = new ForceReplyMarkup { Selective = true };
						break;

					case "Мои акции":
						answerReplyMarkup = GetMyStocksButtons();
						break;

					case "Список моих акций":
						if (user.Stocks.Count != 0)
						{
							answerMessage = String.Empty;
							user.Stocks.ToList().ForEach(s => answerMessage += $"{s.Name} [{s.Symbol}]\n");
						}
						else
							answerMessage = "Вы пока не добавили ни одной акции.";
						answerReplyMarkup = GetMyStocksButtons();
						break;

					case "В главное меню":
						answerMessage = "Возврат в главное меню";
						break;

					case "Рекомендации":
						if (user.Stocks.Count != 0)
						{
							answerMessage = this.GetAdvicesForUserStocks(user);
							answerReplyMarkup = GetMyStocksButtons();
						}
						else
							answerMessage = "Вы пока не добавили ни одной акции.";
						break;

					default:
						if (msg.ReplyToMessage != null && msg.ReplyToMessage.From.Id == TelegramBot.Client.BotId)
						{
							if (msg.ReplyToMessage.Text == "Введите тикер акции для добавления:")
								answerMessage = this.AddStockToUser(user, msg.Text);
							else if (msg.ReplyToMessage.Text == "Введите тикер акции для удаления:")
								answerMessage = this.RemoveStockFromUser(user, msg.Text);

							answerReplyMarkup = GetMyStocksButtons();
						}
						break;
				}

				//Отправляем пользователю сформированный ответ
				watcher.Stop();
				string totalTimeMessage = $"Затраченное время: {Math.Round((double)watcher.ElapsedMilliseconds / 1000, 2)}сек.";

				await TelegramBot.SendMessageAsync(answerChatId, answerMessage, answerReplyMarkup);
				await TelegramBot.SendMessageAsync(answerChatId, totalTimeMessage);
				Logger.CreateLog(new LogRecord
				{ 
					DateTime = DateTime.Now, 
					FromUser = user, 
					Message = $"Запрос: {msg.Text}\n" + $"Ответ: {answerMessage}\n\n" + totalTimeMessage
				}, this.LogRecords);
			}
		}

		//Добавление акции в БД по тикеру через YahooFinance
		private Stock YahooCreateStockBySymbol(string symbol)
		{
			//Timestamp - для оптимизации количества данных, которые подтягиваются через api
			long currentTimestamp = (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI2"]}");

			string response = webClient.DownloadString($"https://yfapi.net/v7/finance/options/{symbol}?date={currentTimestamp}");
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj.optionChain.result;

			//Проверка не пустой ли результат json и подходит ли бумага по параметрам
			if (result.Count == 0 || (bool)result[0].quote.triggerable == false)
				return null;
			//Найденную акцию сразу добавляем в БД и возвращаем ее результатом метода для дальнейшей обработки
			return new StocksRepository(new BaseDataContext()).Create(new Stock { Symbol = symbol.ToUpper(), Name = result[0].quote.shortName });
		}

		private string GetAdviceForStock(Stock stock)
		{
			List<StockQuote> quotes = new StocksQuotesRepository(new BaseDataContext()).GetAll().Where(s => s.Stock.Id == stock.Id).OrderBy(q => q.DateTime).ToList();

			if (quotes.Count == 0)
				return $"У акции {stock.Name} [{stock.Symbol}] нет загруженных котировок";

			//Рассчет индикаторов
			double CCI = Indicators.CalculateCCI(quotes, 50);
			double RSI = Indicators.CalculateRSI(quotes, 14);
			string answer = $"{stock.Name} [{stock.Symbol}] за {quotes.Last().DateTime}: [{quotes.Last().ClosePrice}: {Math.Round(CCI, 2)}, {Math.Round(RSI, 2)}]\n" +
							"Вердикт: ";

			//Формируем вердикт
			if (CCI >= 300 || RSI >= 85)
				answer += "🔴Продавать🔴";
			else if (CCI <= -120 || RSI <= 35)
				answer += "🟢Покупать🟢";
			else
				answer += "⚫️Ждать⚫️";

			return answer;
		}

		//Получаем список советов по всем акциям пользователя
		private string GetAdvicesForUserStocks(User user)
		{
			string answer = String.Empty;

			if (user.Stocks.Count == 0)
				return "У вас пока нет добавленных акций.";

			user.Stocks.ToList().ForEach(s => answer += this.GetAdviceForStock(s) + "\n\n");

			return answer;
		}

		//Получение котировок акции за период через YahooFinance
		private List<StockQuote> GetStockQuotes(List<KeyValuePair<Stock, DateTime>> stocks)
		{
			//Установка интервала и диапазона для выгрузки котировок
			string interval = "1d"; //1m, 5m, 15m, 1d, 1wk, 1mo
			string range = "3mo"; //1d, 5d, 1mo, 3mo, 6mo, 1y, 5y, max

			List<StockQuote> quotes = new List<StockQuote>();
			List<List<Stock>> newStocks = stocks.Select((x, y) => new { Index = y, Value = x.Key })
				.GroupBy(x => x.Index / 20)
				.Select(x => x.Select(y => y.Value).ToList())
				.ToList();

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI2"]}");

			int stock_index = 0;
			foreach (var stocksList in newStocks)
			{
				string symbols = string.Empty;
				stocksList.ForEach(s => symbols += s.Symbol + ",");

				string response = webClient.DownloadString($"https://yfapi.net/v8/finance/spark?interval={interval}&range={range}&symbols={symbols}");
				dynamic obj = JsonConvert.DeserializeObject(response);

				foreach (var stock in stocksList)
				{
					var result = obj[stock.Symbol];

					//Формируем список котировок и возвращаем его
					for (int j = 0; j < result.timestamp.Count; j++)
					{
						DateTime quoteDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((double)result.timestamp[j]).ToLocalTime();
						//Если котировка на более позднюю дату, чем нам нужно - обрабатываем ее
						if (quoteDateTime > stocks[stock_index].Value)
						{
							StockQuote newQuote = new StockQuote { StockId = stock.Id, DateTime = quoteDateTime, ClosePrice = result.close[j] };
							quotes.Add(newQuote);
						}
					}
					stock_index++;
				}
			}
			return quotes;
		}

		private async void SendNotifications(List<Stock> stocks)
		{
			if (stocks.Count == 0)
				return;

			//Брать только тех юзеров, у которых есть хоть одна акция из stocks
			Dictionary<User, string> usersToNotify = new Dictionary<User, string>();
			new UsersRepository(new BaseDataContext()).GetAll().ToList().ForEach(u => usersToNotify.Add(u, String.Empty));
			int notifiedUsers = 0;

			foreach (var stock in stocks)
			{
				List<User> users = usersToNotify.Select(u => u.Key).ToList();
				string advice = this.GetAdviceForStock(stock);

				foreach (var user in users)
				{
					if (user.Stocks.Any(s => s.Id == stock.Id))
						usersToNotify[user] += advice + "\n\n";
				}
			}

			//Проходим по каждому пользователю, которого нужно оповестить и отправляем ему рекомендацию
			foreach (var user in usersToNotify)
			{
				if (user.Value != String.Empty)
				{
					await TelegramBot.SendMessageAsync(user.Key.TelegramId, "Ежедневный отчет по вашим акциям:\n\n" + user.Value);
					notifiedUsers++;
				}
				else if (user.Key.Stocks.Count == 0)
					await TelegramBot.SendMessageAsync(user.Key.TelegramId, "Вы пока не добавили ни одной акции, поэтому ежедневный отчет для вас недоступен.");
			}

			Logger.CreateLog(new LogRecord
			{
				DateTime = DateTime.Now, 
				Message = $"Рассылка рекомендаций успешно завершена. {stocks.Count} акций для {notifiedUsers} пользователей.\n" +
					$"Затрачено времени: {Math.Round((double)this._StopWatcher.ElapsedMilliseconds / 1000, 2)}сек."
			}, this.LogRecords);
		}

		//Подгрузка недостающих котировок по всем акциям (или одной акции) и отправка уведомлений владельцам
		private void LoadMissingQuotes(Stock stock = null)
		{
			//Если аргументом передана конкретная акция - обрабатываем только ее
			//Если нет - обрабатываем все существующие акции
			List<Stock> stocks;
			if (stock == null)
				stocks = new StocksRepository(new BaseDataContext()).GetAll().Where(s => s.Users.Count != 0).ToList();
			else
				stocks = new List<Stock>() { stock };

			List<KeyValuePair<Stock, DateTime>> stocksList = new List<KeyValuePair<Stock, DateTime>>();
			foreach (var s in stocks)
			{
				DateTime lastQuoteDateTime = s.GetDateTimeLastQuote();

				if (this.IsNeedToUploadQuotes(lastQuoteDateTime))
					stocksList.Add(new KeyValuePair<Stock, DateTime>(s, lastQuoteDateTime));
			}
			new StocksQuotesRepository(new BaseDataContext()).Create(this.GetStockQuotes(stocksList));


			if (stocksList.Count != 0)
				this.SendNotifications(stocksList.Select(s => s.Key).Where(s => s.StockQuotes.Count != 0).ToList());
		}

		//Регистрируем пользователя с соответствующим именем и telegramId
		private User RegisterUser(string username, string telegramId)
		{
			//Добавляем нового пользователя через репозиторий и возвращаем ссылку на него
			return new UsersRepository(new BaseDataContext()).Create(new User { Username = username, TelegramId = telegramId });
		}

		//Добавление акции по тикеру для пользователя
		private string AddStockToUser(User user, string symbol)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = new StocksRepository(new BaseDataContext()).GetAll().FirstOrDefault(s => s.Symbol.ToUpper() == symbol.ToUpper());
			Stock createdStock;

			//Если находим - делаем связку пользователя и найденной акции
			if (newStock != null)
			{
				//Если у этого пользователя нет такой акции - добавляем, если есть - выводим ошибку
				if (user.Stocks.Count(s => s.Symbol.ToUpper() == symbol.ToUpper()) == 0)
				{
					new UsersRepository(new BaseDataContext()).AddStock(user.Id, newStock.Id);
					return $"Бумага {newStock.Symbol} успешно добавлена из БД!";
				}
				else
					return "Данная акция уже числится на вашем аккаунте.";
			}
			//Если не находим акцию в БД - ищем такой тикер через YahooFinance и добавляем к пользователю
			else if ((createdStock = YahooCreateStockBySymbol(symbol)) != null)
			{
				//Добавляем акцию к пользователю (обязательно через Id)
				new UsersRepository(new BaseDataContext()).AddStock(user.Id, createdStock.Id);
				//Подгружаем все недостающие котировки по только что созданной акции
				this.LoadMissingQuotes(createdStock);
				return $"Бумага {createdStock.Symbol} успешно добавлена из YahooFinance!";
			}
			//Не находим такой тикер на YahooFinance и в БД - сообщение об ошибке
			return "При добавлении тикера произошла ошибка. Пожалуйста, вводите тикеры ТОЛЬКО с сайта finance.yahoo.com";
		}

		//Удаление акции по тикеру у пользователя
		private string RemoveStockFromUser(User user, string symbol)
		{
			//Ищем акцию с таким тикером у пользователя и если нашли - удаляем
			Stock stockToRemove = user.Stocks.FirstOrDefault(s => s.Symbol == symbol.ToUpper());
			if (stockToRemove != null)
			{
				//Удаляем обязательно через Id
				new UsersRepository(new BaseDataContext()).RemoveStock(user.Id, stockToRemove.Id);
				return "Данная акция успешно удалена с вашего аккаунта.";
			}
			else
				return "Акция с таким тикером не числится на вашем аккаунте!";
		}

		#region Разметки клавиатуры
		//Главное меню
		private static IReplyMarkup GetMainMenuButtons()
		{
			return new ReplyKeyboardMarkup
			{
				Keyboard = new List<List<KeyboardButton>>
				{
					new List<KeyboardButton>{ new KeyboardButton { Text = "Мои акции"} },
					new List<KeyboardButton>{ new KeyboardButton { Text = "Настройки"} }
				},
				ResizeKeyboard = true
			};
		}

		//Меню "Акции"
		private static IReplyMarkup GetMyStocksButtons()
		{
			return new ReplyKeyboardMarkup
			{
				Keyboard = new List<List<KeyboardButton>>
				{
					new List<KeyboardButton> { new KeyboardButton { Text = "Добавить акцию" }, new KeyboardButton { Text = "Удалить акцию" } },
					new List<KeyboardButton> { new KeyboardButton { Text = "Список моих акций" } },
					new List<KeyboardButton> { new KeyboardButton { Text = "Рекомендации" } },
					new List<KeyboardButton> { new KeyboardButton { Text = "В главное меню" } },
				},
				ResizeKeyboard = true
			};
		}
		#endregion

		#region Вспомогательные методы
		//Проверка нужно ли загружать котировки
		private bool IsNeedToUploadQuotes(DateTime date)
		{
			//Если дата котировки меньше текущей больше чем на один день, то нужно загрузить котировки
			if (date.AddDays(1) < DateTime.Now)
				return true;
			return false;
		}
		#endregion
		#endregion
	}
}
