using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StocksHelper.Commands;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories;
using StocksHelper.Repositories.Base;
using StocksHelper.ViewModels.Base;
using StocksHelper.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Input;
using System.Windows.Forms;
using System.Configuration;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using OxyPlot;
using OxyPlot.Axes;

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{
			this.NewStock = new Stock();
			this.DBContext = new BaseDataContext();
			this.MainContentControl = new AuthenticationViewModel(this);
			this._UsersRepository = new UsersRepository(this.DBContext);
			this._StocksRepository = new StocksRepository(this.DBContext);
			this._StockQuotesRepository = new StocksQuotesRepository(this.DBContext);
			this.BotClient = new TelegramBotClient(ConfigurationManager.AppSettings["TelegramBotAPI"]);

			ShowNewStockWindowCommand = new RelayCommand(OnShowNewStockWindowCommandExecuted, CanShowNewStockWindowCommandExecute);
			AddStockCommand = new RelayCommand(OnAddStockCommandExecuted, CanAddStockCommandExecute);
			RemoveStockCommand = new RelayCommand(OnRemoveStockCommandExecuted, CanRemoveStockCommandExecute);

			this.LoadMissingQuotes();
			this.ConfigureTelegramBot(this.BotClient);
		}
		#endregion

		#region Properties
		private object _mainContentControl;
		public object MainContentControl
		{
			get => _mainContentControl;
			set => Set(ref _mainContentControl, value);
		}

		private readonly BaseDataContext DBContext;
		private UsersRepository _UsersRepository;
		private IRepository<Stock> _StocksRepository;
		private IRepository<StockQuote> _StockQuotesRepository;

		private User _LoggedInUser;
		public User LoggedInUser
		{
			get => _LoggedInUser;
			set
			{
				Set(ref _LoggedInUser, value);
				this.MainContentControl = null;
			}
		}

		private Stock _SelectedStock;
		public Stock SelectedStock
		{
			get => _SelectedStock;
			set
			{
				Set(ref _SelectedStock, value);
				this.MainContentControl = new StocksViewModel(this.SelectedStock);
			}
		}

		private Stock _NewStock;
		public Stock NewStock
		{
			get => _NewStock;
			set => Set(ref _NewStock, value);
		}

		TelegramBotClient BotClient;
		#endregion

		#region Commands
		//Вывод окна для добавления новой акции
		public ICommand ShowNewStockWindowCommand { get; }
		private bool CanShowNewStockWindowCommandExecute(object p) => this.LoggedInUser != null;
		public void OnShowNewStockWindowCommandExecuted(object p)
		{
			NewStockWindow newWindow = new NewStockWindow(this);
			newWindow.ShowDialog();
		}

		//Добавление новой акции к аккаунту пользователя
		public ICommand AddStockCommand { get; }
		private bool CanAddStockCommandExecute(object p) => this.LoggedInUser != null;
		public void OnAddStockCommandExecuted(object p)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol.ToUpper() == this.NewStock.Symbol.ToUpper());
			Stock createdStock;

			//Если находим - делаем связку пользователя и найденной акции
			if (newStock != null)
			{
				if (this.LoggedInUser.Stocks.Count(s => s.Symbol.ToUpper() == this.NewStock.Symbol.ToUpper()) == 0)
					AddStockToUser(newStock);
				else
					MessageBox.Show("Данная акция уже числится на вашем аккаунте.");
			}
			//Если не находим акцию в БД - ищем такой тикер через YahooFinance и выполняем связку
			else if ((createdStock = YahooCreateStockBySymbol(this.NewStock.Symbol)) != null)
				AddStockToUser(createdStock);
			//Не находим такой тикер на YahooFinance - сообщение об ошибке
			else
				MessageBox.Show("Данный тикер не найден.");
		}

		//Удаление выбранной акции с аккаунта пользователя
		public ICommand RemoveStockCommand { get; }
		private bool CanRemoveStockCommandExecute(object p) => this.SelectedStock != null;
		public void OnRemoveStockCommandExecuted(object p)
		{
			DialogResult dialogResult = MessageBox.Show($"Вы действительно хотите удалить акцию {SelectedStock.Name}[{SelectedStock.Symbol}]?",
																	"Удаление акции",
																	MessageBoxButtons.YesNo);

			if (dialogResult == DialogResult.Yes)
			{
				//Сначала удаляем акцию из БД, затем из программы
				this._UsersRepository.RemoveStock(this.LoggedInUser.Id, this.SelectedStock.Id);
				this.LoggedInUser.Stocks.Remove(this.SelectedStock);

				MessageBox.Show("Акция удалена.");
			}
		}
		#endregion

		#region Methods
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

		//Привязка акции к аккаунту пользователя
		private void AddStockToUser(Stock stock)
		{
			this.LoggedInUser.Stocks.Add(stock);
			this._UsersRepository.AddStock(this.LoggedInUser, stock);
			MessageBox.Show($"Бумага {stock.Symbol} успешно добавлена!");
		}

		private async void SendNotifications(Stock stock)
		{
			List<User> usersToNotify = this._UsersRepository.GetAll().Where(u => u.Stocks.Contains(stock)).ToList();
			List<DataPoint> quotes = new List<DataPoint>();
			this._StockQuotesRepository.GetAll().Where(s => s.Stock.Id == stock.Id).OrderBy(q => q.DateTime).ToList().ForEach(q => quotes.Add(new DataPoint(DateTimeAxis.ToDouble(q.DateTime), q.ClosePrice)));

			double CCI = Indicators.CalculateCCI(quotes, 50).Last().Y;
			double RSI = Indicators.CalculateRSI(quotes, 14).Last().Y;
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

		private void LoadMissingQuotes()
		{
			//Проходим по всем акциям и проверяем время их последних котировок. Если оно ме
			List<Stock> stocks = this._StocksRepository.GetAll().ToList();
			for (int i = 0; i < stocks.Count; i++)
			{
				//Получаем дату последней котировки
				DateTime lastQuoteDateTime = stocks[i].GetDateTimeLastQuote();

				bool hasNewQuotes = false;

				//Если у акции нет котировок или если день, следующий за датой последней котировки НЕ выходной, то подгружаем котировки
				if (!this.IsWeekendDay(stocks[i].GetDateTimeLastQuote().AddDays(1)))
				{
					this._StockQuotesRepository.Create(this.GetStockQuotes(stocks[i], lastQuoteDateTime));
					hasNewQuotes = true;
				}

				if (hasNewQuotes)
					this.SendNotifications(stocks[i]);
			}
		}

		private bool IsWeekendDay(DateTime date)
		{
			if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
				return true;
			return false;
		}

		private void ConfigureTelegramBot(TelegramBotClient botClient)
		{
			botClient.StartReceiving();
			botClient.OnMessage += OnMessageHandler;
		}

		private async void OnMessageHandler(object sender, MessageEventArgs e)
		{
			var msg = e.Message;
			string answer = "Выберите команду:";
			User user;

			//Зарегистрировать пользователя, если он не найден
			if (this.RegisterUser(msg.From.Username, msg.From.Id.ToString()))
			{
				answer = $"Добро пожаловать! Я - ваш новый личный помощник на фондовом рынке. Все, что вам нужно - это сформировать список " +
					"интересующих вас акций через интерфейс ниже. После добавления акций, я буду присылать вам ежедневные отчеты и сигналы по выбранным компаниям.";
				await this.BotClient.SendTextMessageAsync
				(
					chatId: msg.Chat.Id,
					text: answer,
					replyMarkup: GetMainMenuButtons()
				);
			}
			user = this._UsersRepository.GetAll().FirstOrDefault(u => u.TelegramId == msg.From.Id.ToString());

			if (msg.Text != null)
			{
				switch (msg.Text)
				{
					case "Добавить акцию" or "Удалить акцию":
						if (msg.Text == "Добавить акцию")
							answer = "Введите тикер акции для добавления:";
						else
							answer = "Введите тикер акции для удаления:";
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: answer,
							replyMarkup: new ForceReplyMarkup { Selective = true }						
						);
						break;

					case "Мои акции":
						/*if (user.Stocks.Count != 0)
							user.Stocks.ToList().ForEach(s => answer += $"{s.Name} [{s.Symbol}]\n");
						else
							answer = "Вы пока не добавили ни одной акции.";*/
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: answer,
							replyMarkup: GetMyStocksButtons()
						);
						break;

					case "Список моих акций":
						if (user.Stocks.Count != 0)
						{
							answer = String.Empty;
							user.Stocks.ToList().ForEach(s => answer += $"{s.Name} [{s.Symbol}]\n");
						}
						else
							answer = "Вы пока не добавили ни одной акции.";
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: answer,
							replyMarkup: GetMyStocksButtons()
						);
						break;

					case "В главное меню":
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: "Возврат в главное меню",
							replyMarkup: GetMainMenuButtons()
						);
						break;

					default:
						if (msg.ReplyToMessage != null && msg.ReplyToMessage.From.Id == this.BotClient.BotId)
						{
							if (msg.ReplyToMessage.Text == "Введите тикер акции для добавления:")
								answer = this.AddStockToUser(user, msg.Text);
							else if (msg.ReplyToMessage.Text == "Введите тикер акции для удаления:")
								answer = this.RemoveStockFromUser(user, msg.Text);

							await this.BotClient.SendTextMessageAsync(msg.Chat.Id, answer, replyMarkup: GetMyStocksButtons());
						}
						else
							await this.BotClient.SendTextMessageAsync(msg.Chat.Id, answer, replyMarkup: GetMainMenuButtons());
						break;
				}
			}
		}

		private static IReplyMarkup GetMainMenuButtons()
		{
			return new ReplyKeyboardMarkup
			{
				Keyboard = new List<List<KeyboardButton>>
				{
					new List<KeyboardButton>{ new KeyboardButton { Text = "Мои акции"} },
				}
			};
		}

		private static IReplyMarkup GetMyStocksButtons()
		{
			return new ReplyKeyboardMarkup
			{
				Keyboard = new List<List<KeyboardButton>>
				{
					new List<KeyboardButton>{ new KeyboardButton { Text = "Добавить акцию"}, new KeyboardButton { Text = "Удалить акцию" } },
					new List<KeyboardButton>{ new KeyboardButton { Text = "Список моих акций"} },
					new List<KeyboardButton>{ new KeyboardButton { Text = "В главное меню"} },
				}
			};
		}

		private bool RegisterUser(string username, string telegramId)
		{
			if (this._UsersRepository.GetAll().FirstOrDefault(u => u.TelegramId == telegramId) == null)
			{
				this._UsersRepository.Create(new User { Username = username, TelegramId = telegramId });
				return true;
			}
			return false;
		}

		private string AddStockToUser(User user, string symbol)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol.ToUpper() == symbol.ToUpper());
			Stock createdStock;

			//Если находим - делаем связку пользователя и найденной акции
			if (newStock != null)
			{
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
				//Добавляем акцию в репозиторий и сразу проверяем недостающие котировки
				this._UsersRepository.AddStock(user, createdStock);
				this.LoadMissingQuotes();
				return $"Бумага {createdStock.Symbol} успешно добавлена из YahooFinance!";
			}
			//Не находим такой тикер на YahooFinance - сообщение об ошибке
			return "При добавлении тикера произошла ошибка. Пожалуйста, вводите тикеры ТОЛЬКО с сайта finance.yahoo.com";
		}

		private string RemoveStockFromUser(User user, string symbol)
		{
			Stock stockToRemove = user.Stocks.FirstOrDefault(s => s.Symbol == symbol.ToUpper());

			if (stockToRemove != null)
			{
				this._UsersRepository.RemoveStock(user.Id, stockToRemove.Id);
				return "Данная акция успешно удалена с вашего аккаунта.";
			}
			else
				return "Акция с таким тикером не числится на вашем аккаунте!";
		}
		#endregion
	}
}
