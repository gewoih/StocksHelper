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

			ShowNewStockWindowCommand = new RelayCommand(OnShowNewStockWindowCommandExecuted, CanShowNewStockWindowCommandExecute);
			AddStockCommand = new RelayCommand(OnAddStockCommandExecuted, CanAddStockCommandExecute);
			LoadStockQuotesCommand = new RelayCommand(OnLoadStockQuotesCommandExecuted, CanLoadStockQuotesCommandExecute);
			RemoveStockCommand = new RelayCommand(OnRemoveStockCommandExecuted, CanRemoveStockCommandExecute);
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
				this.MainContentControl = new StocksViewModel(this.SelectedStock);
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

		//Загрузка котировок в БД для выбранной акции
		public ICommand LoadStockQuotesCommand { get; }
		private bool CanLoadStockQuotesCommandExecute(object p) => this.SelectedStock != null;
		public void OnLoadStockQuotesCommandExecuted(object p)
		{
			this._StockQuotesRepository.Create(this.GetStockQuotes(this.SelectedStock).ToArray());
			MessageBox.Show("Котировки загружены!");
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

		//Получение котировок акции за год через YahooFinance
		private List<StockQuote> GetStockQuotes(Stock stock)
		{
			//Установка интервала и диапазона для выгрузки котировок
			string interval = "1d"; //1m, 5m, 15m, 1d, 1wk, 1mo
			string range = "1y"; //1d, 5d, 1m, 3m, 6m, 1y, 5y, max

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add($"X-API-KEY: {ConfigurationManager.AppSettings["YahooFinanceAPI1"]}");

			string response = webClient.DownloadString($"https://yfapi.net/v8/finance/spark?interval={interval}&range={range}&symbols={this.SelectedStock.Symbol}");
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj[this.SelectedStock.Symbol];

			List<StockQuote> quotes = new List<StockQuote>();
			for (int i = 0; i < result.timestamp.Count; i++)
				quotes.Add(new StockQuote { StockId = stock.Id, DateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((double)result.timestamp[i]).ToLocalTime(), ClosePrice = result.close[i] });

			return quotes;
		}
		#endregion
	}
}
