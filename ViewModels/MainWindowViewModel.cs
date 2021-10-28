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
using System.Windows;
using System.Windows.Input;

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{
			this.NewStock = new Stock();
			this.MainContentControl = new AuthenticationViewModel(this);
			this._StocksRepository = new StocksRepository(new BaseDataContext());
			this._UsersRepository = new UsersRepository(new BaseDataContext());

			ShowNewStockWindowCommand = new RelayCommand(OnShowNewStockWindowCommandExecuted, CanShowNewStockWindowCommandExecute);
			AddStockCommand = new RelayCommand(OnAddStockCommandExecuted, CanAddStockCommandExecute);
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

		private IRepository<Stock> _StocksRepository;
		private IRepository<User> _UsersRepository;

		private User _LoggedInUser;
		public User LoggedInUser
		{
			get => _LoggedInUser;
			set
			{
				Set(ref _LoggedInUser, value);
				this.MainContentControl = new StocksViewModel(this.LoggedInUser);
			}
		}

		private Stock _SelectedStock;
		public Stock SelectedStock
		{
			get => _SelectedStock;
			set => Set(ref _SelectedStock, value);
		}

		private Stock _NewStock;
		public Stock NewStock
		{
			get => _NewStock;
			set => Set(ref _NewStock, value);
		}
		#endregion

		#region Commands
		public ICommand ShowNewStockWindowCommand { get; }
		private bool CanShowNewStockWindowCommandExecute(object p) => this.LoggedInUser != null;
		public void OnShowNewStockWindowCommandExecuted(object p)
		{
			NewStockWindow newWindow = new NewStockWindow(this);
			newWindow.ShowDialog();
		}

		public ICommand AddStockCommand { get; }
		private bool CanAddStockCommandExecute(object p) => this.LoggedInUser != null;
		public void OnAddStockCommandExecuted(object p)
		{
			//Ищем акцию с таким тикером в БД
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol == this.NewStock.Symbol);
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

		public ICommand RemoveStockCommand { get; }
		private bool CanRemoveStockCommandExecute(object p) => this.SelectedStock != null;
		public void OnRemoveStockCommandExecuted(object p)
		{
			MessageBox.Show("");
		}
		#endregion

		#region Methods
		private Stock YahooCreateStockBySymbol(string symbol)
		{
			//Timestamp - для оптимизации количества данных, которые подтягиваются через api
			long currentTimestamp = (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

			WebClient webClient = new WebClient();
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add("X-API-KEY: npF3vHM9Ga5sBvRIT9tHd8chMYxUdwzV7I7WXDMZ");

			string response = webClient.DownloadString($"https://yfapi.net/v7/finance/options/{symbol}?date={currentTimestamp}");
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj.optionChain.result;
			if (result.Count == 0)
				return null;
			//Найденную акцию сразу добавляем в БД и возвращаем ее результатом метода для дальнейшей обработки
			return this._StocksRepository.Create(new Stock { Symbol = symbol.ToUpper(), Name = result[0].quote.shortName });
		}

		private void AddStockToUser(Stock stock)
		{
			this.LoggedInUser.Stocks.Add(stock);
			this._UsersRepository.Update(this.LoggedInUser.Id, this.LoggedInUser);
			MessageBox.Show($"Бумага {stock.Symbol} успешно добавлена!");
		}
		#endregion
	}
}
