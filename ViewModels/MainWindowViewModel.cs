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

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{
			this._BaseDataContext = new BaseDataContext();
			this._UsersRepository = new UsersRepository(this._BaseDataContext);
			this._StocksRepository = new StocksRepository(this._BaseDataContext);
			this._StockQuotesRepository = new StocksQuotesRepository(this._BaseDataContext);
			this._LogRecordsRepository = new LogRecordsRepository(this._BaseDataContext);
			this.BotClient = new TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotAPI"]);

			this.ConfigureTelegramBot(this.BotClient);
			//Загрузка логов
			this.LogRecords = new ObservableCollection<LogRecord>(this._LogRecordsRepository.GetAll());
			//Загружаем недостающие котировки по всем акциям при запуске приложения
			this.LoadMissingQuotes();
		}
		#endregion

		#region Properties
		private TelegramBotClient BotClient;

		private readonly BaseDataContext _BaseDataContext;
		private readonly UsersRepository _UsersRepository;
		private readonly StocksRepository _StocksRepository;
		private readonly StocksQuotesRepository _StockQuotesRepository;
		private readonly LogRecordsRepository _LogRecordsRepository;

		private ObservableCollection<LogRecord> _LogRecords;
		public ObservableCollection<LogRecord> LogRecords
		{
			get => _LogRecords;
			set => Set(ref _LogRecords, value);
		}
		#endregion

		#region Methods
		private void ConfigureTelegramBot(TelegramBotClient botClient)
		{
			botClient.StartReceiving();
			botClient.OnMessage += OnMessageHandler;
		}

		private async void OnMessageHandler(object sender, MessageEventArgs e)
		{
			var msg = e.Message;
			Telegram.Bot.Types.ChatId answerChatId = msg.Chat.Id;
			string answerMessage = "Выберите команду:";
			IReplyMarkup answerReplyMarkup = GetMainMenuButtons();
			//Ищем пользователя по telegramId
			User user = this._UsersRepository.GetAll().FirstOrDefault(u => u.TelegramId == msg.From.Id.ToString());

			//Зарегистрировать пользователя, если он не найден
			if (user == null)
			{
				user = this.RegisterUser(msg.From.Username, msg.From.Id.ToString());

				answerMessage = $"Добро пожаловать! Я - ваш новый личный помощник на фондовом рынке. Все, что вам нужно - это сформировать список " +
					"интересующих вас акций через интерфейс ниже. После добавления акций, я буду присылать вам ежедневные отчеты и сигналы по выбранным компаниям.";
			}

			if (msg.Text != null)
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

					default:
						if (msg.ReplyToMessage != null && msg.ReplyToMessage.From.Id == this.BotClient.BotId)
						{
							if (msg.ReplyToMessage.Text == "Введите тикер акции для добавления:")
								answerMessage = this.AddStockToUser(user, msg.Text);
							else if (msg.ReplyToMessage.Text == "Введите тикер акции для удаления:")
								answerMessage = this.RemoveStockFromUser(user, msg.Text);

							answerReplyMarkup = GetMyStocksButtons();
						}
						break;
				}

				await this.BotClient.SendTextMessageAsync
				(
					chatId: answerChatId,
					text: answerMessage,
					replyMarkup: answerReplyMarkup
				);

				Dispatcher.CurrentDispatcher.Invoke
				(
					new Action(() =>
					{
						this.LogRecords.Add(this._LogRecordsRepository.Create(new LogRecord { DateTime = DateTime.Now, FromUser = user, Message = msg.Text }));
						this.LogRecords.Add(this._LogRecordsRepository.Create(new LogRecord { DateTime = DateTime.Now, ToUser = user, Message = answerMessage }));
					}),
					DispatcherPriority.Normal
				);
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
			if (result.Count == 0 || (bool)result[0].quote.triggerable == false)
				return null;
			//Найденную акцию сразу добавляем в БД и возвращаем ее результатом метода для дальнейшей обработки
			return this._StocksRepository.Create(new Stock { Symbol = symbol.ToUpper(), Name = result[0].quote.shortName });
		}

		private async void SendNotifications(Stock stock, List<User> usersToNotify = null)
		{
			//Список пользователей, которым нужно прислать уведомление
			//Если его не передали аргументом - берем всех пользователей, у которых есть заданная акций
			if (usersToNotify == null)
				usersToNotify = this._UsersRepository.GetAll().Where(u => u.Stocks.Contains(stock)).ToList();
			//Список котировок акции
			List<DataPoint> quotes = new List<DataPoint>();
			this._StockQuotesRepository.GetAll().Where(s => s.Stock.Id == stock.Id).OrderBy(q => q.DateTime).ToList().ForEach(q => quotes.Add(new DataPoint(q.DateTime.ToOADate(), q.ClosePrice)));

			//Рассчет индикаторов
			double CCI = Indicators.CalculateCCI(quotes, 50);
			double RSI = Indicators.CalculateRSI(quotes, 14);
			string answer = $"Показатели по акции {stock.Name} [{stock.Symbol}] за {stock.StockQuotes.Last().DateTime.ToString("D")}:\n" +
							$"CCI: {Math.Round(CCI, 2)}\n" +
							$"RSI: {Math.Round(RSI, 2)}\n" +
							"Вердикт: ";

			if (CCI + RSI >= 400)
				answer += "🔴Настоятельно рекомендуем зафиксировать прибыль по данной акции!🔴";
			else if (CCI + RSI >= 250)
				answer += "🟡Возможно стоит зафиксировать прибыль по данной акции🟡";
			else if (CCI + RSI <= -150)
				answer += "🟢Настоятельно рекомендуем докупить акции этой компании!🟢";
			else if (CCI + RSI <= -100)
				answer += "🟡Возможно стоит усредниться и взять пару акций данной компании🟡";
			else
				answer += "⚫️Не рекомендуется предпринимать действий по этой акции⚫️";

			foreach (var user in usersToNotify)
			{
				await this.BotClient.SendTextMessageAsync
				(
					chatId: user.TelegramId,
					text: answer
				);
			}
		}

		//Получение котировок акции за период через YahooFinance
		private List<StockQuote> GetStockQuotes(Stock stock, DateTime fromDateTime)
		{
			//Установка интервала и диапазона для выгрузки котировок
			string interval = "1d"; //1m, 5m, 15m, 1d, 1wk, 1mo
			string range = "3mo"; //1d, 5d, 1mo, 3mo, 6mo, 1y, 5y, max

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI2"]}");

			string response = webClient.DownloadString($"https://yfapi.net/v8/finance/spark?interval={interval}&range={range}&symbols={stock.Symbol}");
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj[stock.Symbol];

			//Формируем список котировок и возвращаем его
			List<StockQuote> quotes = new List<StockQuote>();
			for (int i = 0; i < result.timestamp.Count; i++)
			{
				DateTime quoteDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((double)result.timestamp[i]).ToLocalTime();
				if (quoteDateTime > fromDateTime)
				{
					StockQuote newQuote = new StockQuote { StockId = stock.Id, DateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((double)result.timestamp[i]).ToLocalTime(), ClosePrice = result.close[i] };
					quotes.Add(newQuote);
				}
			}
			return quotes;
		}

		//Подгрузка недостающих котировок по всем акциям (или одной акции) и отправка уведомлений владельцам
		private void LoadMissingQuotes(Stock stock = null)
		{
			//Если аргументом передана конкретная акция - обрабатываем только ее
			//Если нет - обрабатываем все существующие акции
			List<Stock> stocks;
			if (stock == null)
				stocks = this._StocksRepository.GetAll().ToList();
			else
				stocks = new List<Stock>() { stock };

			//Проходим по всем акциям и проверяем время их последних котировок
			for (int i = 0; i < stocks.Count; i++)
			{
				//Получаем дату последней котировки
				DateTime lastQuoteDateTime = stocks[i].GetDateTimeLastQuote();

				//Флаг есть ли у акции новые котировки
				bool hasNewQuotes = false;

				//Если у акции нет котировок или если день, СЛЕДУЮЩИЙ за датой последней котировки НЕ выходной, то подгружаем котировки
				//ПЕРЕДЕЛАТЬ МЕТОД ISWEEKENDDAY. НЕПРАВИЛЬНАЯ ЛОГИКА.
				if (stocks[i].StockQuotes.Count == 0 || !this.IsWeekendDay(stocks[i].GetDateTimeLastQuote().AddDays(1)))
				{
					this._StockQuotesRepository.Create(this.GetStockQuotes(stocks[i], lastQuoteDateTime));
					hasNewQuotes = true;
				}

				//Если у акции есть новые котировки - отправляем уведомления всем владельцам этой акции
				if (hasNewQuotes)
					this.SendNotifications(stocks[i]);
			}
		}

		//Регистрируем пользователя с соответствующим именем и telegramId
		private User RegisterUser(string username, string telegramId)
		{
			//Добавляем нового пользователя через репозиторий и возвращаем ссылку на него
			return this._UsersRepository.Create(new User { Username = username, TelegramId = telegramId });
		}

		//Добавление акции по тикеру для пользователя
		private string AddStockToUser(User user, string symbol)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol.ToUpper() == symbol.ToUpper());
			Stock createdStock;

			//Если находим - делаем связку пользователя и найденной акции
			if (newStock != null)
			{
				//Если у этого пользователя нет такой акции - добавляем, если есть - выводим ошибку
				if (user.Stocks.Count(s => s.Symbol.ToUpper() == symbol.ToUpper()) == 0)
				{
					this._UsersRepository.AddStock(user, newStock);
					return $"Бумага {newStock.Symbol} успешно добавлена из БД!";
				}
				else
					return "Данная акция уже числится на вашем аккаунте.";
			}
			//Если не находим акцию в БД - ищем такой тикер через YahooFinance и выполняем связку
			else if ((createdStock = YahooCreateStockBySymbol(symbol)) != null)
			{
				//Добавляем акцию к пользователю
				this._UsersRepository.AddStock(user, createdStock);
				//Подгружаем все недостающие котировки
				this.LoadMissingQuotes();
				return $"Бумага {createdStock.Symbol} успешно добавлена из YahooFinance!";
			}
			//Не находим такой тикер на YahooFinance - сообщение об ошибке
			return "При добавлении тикера произошла ошибка. Пожалуйста, вводите тикеры ТОЛЬКО с сайта finance.yahoo.com";
		}

		//Удаление акции по тикеру у пользователя
		private string RemoveStockFromUser(User user, string symbol)
		{
			//Ищем акцию с таким тикером у пользователя и если нашли - удаляем
			Stock stockToRemove = user.Stocks.FirstOrDefault(s => s.Symbol == symbol.ToUpper());
			if (stockToRemove != null)
			{
				this._UsersRepository.RemoveStock(user.Id, stockToRemove.Id);
				return "Данная акция успешно удалена с вашего аккаунта.";
			}
			else
				return "Акция с таким тикером не числится на вашем аккаунте!";
		}

#region Разметки клавиатуры
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
		//Проверка является ли дата выходным днем
		private bool IsWeekendDay(DateTime date)
		{
			if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
				return true;
			return false;
		}
#endregion
#endregion
	}
}
