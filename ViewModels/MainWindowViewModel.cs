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
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI1"]}");

			string response = webClient.DownloadString($"https://yfapi.net/v7/finance/options/{symbol}?date={currentTimestamp}");
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj.optionChain.result;
			if (result.Count == 0)
				return null;
			//Найденную акцию сразу добавляем в БД и возвращаем ее результатом метода для дальнейшей обработки
			//Небольшой костыль в виде создания нового репозитория. Через this._StocksRepository по какой-то причине не работает:(
			return this._StocksRepository.Create(new Stock { Symbol = symbol.ToUpper(), Name = result[0].quote.shortName });
		}

		//Привязка акции к аккаунту пользователя
		private void AddStockToUser(Stock stock)
		{
			this.LoggedInUser.Stocks.Add(stock);
			this._UsersRepository.AddStock(this.LoggedInUser, stock);
			MessageBox.Show($"Бумага {stock.Symbol} успешно добавлена!");
		}

		private void SendNotifications(Stock stock)
		{
			List<User> usersToNotify = this._UsersRepository.GetAll().Where(u => u.Stocks.Contains(stock)).ToList();
			double CCI = Indicators.CalculateCCI();
			double RSI;
		}

		//Получение котировок акции за период через YahooFinance
		private List<StockQuote> GetStockQuotes(Stock stock, DateTime fromDateTime)
		{
			//Установка интервала и диапазона для выгрузки котировок
			string interval = "1d"; //1m, 5m, 15m, 1d, 1wk, 1mo
			string range = "3mo"; //1d, 5d, 1mo, 3mo, 6mo, 1y, 5y, max

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI1"]}");

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

					if (i == result.timestamp.Count - 1)
						this.SendNotifications(stock);
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
			string answer = "";
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
					replyMarkup: GetButtons()
				);
			}
			user = this._UsersRepository.GetAll().FirstOrDefault(u => u.TelegramId == msg.From.Id.ToString());

			if (msg.Text != null)
			{
				switch (msg.Text)
				{
					case "Добавить акцию":
						answer = "Введите тикер акции:";
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: answer,
							replyMarkup: new ForceReplyMarkup { Selective = true }						
						);
						break;

					case "Мои акции":
						if (user.Stocks.Count != 0)
							user.Stocks.ToList().ForEach(s => answer += $"{s.Name} [{s.Symbol}]\n");
						else
							answer = "Вы пока не добавили ни одной акции.";
						await this.BotClient.SendTextMessageAsync
						(
							chatId: msg.Chat.Id,
							text: answer
						);
						break;

					default:
						if (msg.ReplyToMessage != null && msg.ReplyToMessage.From.Id == this.BotClient.BotId && msg.ReplyToMessage.Text == "Введите тикер акции:")
						{
							answer = this.AddStockToUser(user, new Stock { Symbol = msg.Text });
							await this.BotClient.SendTextMessageAsync(msg.Chat.Id, answer, replyMarkup: GetButtons());
						}
						else
							await this.BotClient.SendTextMessageAsync(msg.Chat.Id, "Выберите команду: ", replyMarkup: GetButtons());
						break;
				}
			}
		}

		private static IReplyMarkup GetButtons()
		{
			return new ReplyKeyboardMarkup
			{
				Keyboard = new List<List<KeyboardButton>>
				{
					new List<KeyboardButton>{ new KeyboardButton { Text = "Добавить акцию"}, new KeyboardButton { Text = "Мои акции" } },
					new List<KeyboardButton>{ new KeyboardButton { Text = "Проверить подписку"} },
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

		private string AddStockToUser(User user, Stock stock)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol.ToUpper() == stock.Symbol.ToUpper());
			Stock createdStock;

			//Если находим - делаем связку пользователя и найденной акции
			if (newStock != null)
			{
				if (user.Stocks.Count(s => s.Symbol.ToUpper() == stock.Symbol.ToUpper()) == 0)
				{
					this._UsersRepository.AddStock(user, newStock);
					return $"Бумага {newStock.Symbol} успешно добавлена из БД!";
				}
				else
					return "Данная акция уже числится на вашем аккаунте.";
			}
			//Если не находим акцию в БД - ищем такой тикер через YahooFinance и выполняем связку
			else if ((createdStock = YahooCreateStockBySymbol(stock.Symbol)) != null)
			{
				//Добавляем акцию в репозиторий и сразу проверяем недостающие котировки
				this._UsersRepository.AddStock(user, createdStock);
				this.LoadMissingQuotes();
				return $"Бумага {createdStock.Symbol} успешно добавлена из YahooFinance!";
			}
			//Не находим такой тикер на YahooFinance - сообщение об ошибке
			return "Данный тикер не найден. Пожалуйста, вводите тикеры ТОЛЬКО с сайта finance.yahoo.com";
		}
		#endregion
	}
}
