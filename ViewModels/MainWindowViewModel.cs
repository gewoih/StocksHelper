using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StocksHelper.Commands;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories;
using StocksHelper.Repositories.Base;
using StocksHelper.ViewModels.Base;
using StocksHelper.Views;
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

			ShowNewStockWindowCommand = new RelayCommand(OnShowNewStockWindowCommandExecuted, CanShowNewStockWindowCommandExecute);
			AddStockCommand = new RelayCommand(OnAddStockCommandExecuted, CanAddStockCommandExecute);
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
			Stock newStock = this._StocksRepository.GetAll().FirstOrDefault(s => s.Symbol == this.NewStock.Symbol);
			if (newStock != null)
			{
				if (this.LoggedInUser.Stocks.Count(s => s.Symbol.ToUpper() == this.NewStock.Symbol.ToUpper()) == 0)
					this.LoggedInUser.Stocks.Add(newStock);
				else
					MessageBox.Show("Данная акция уже числится на вашем аккаунте.");
			}
			else if (YahooFindSymbol(this.NewStock.Symbol))
			{
				MessageBox.Show("Данная акция найдена на YahooFinance.");
			}
			else
				MessageBox.Show("Данный тикер не найден.");
		}

		public ICommand RemoveStockCommand { get; }
		private bool CanRemoveStockCommandExecute(object p) => this.SelectedStock != null;
		public void OnRemoveStockCommandExecuted(object p)
		{

		}
		#endregion

		#region Methods
		private bool YahooFindSymbol(string symbol)
		{
			WebClient webClient = new WebClient();
			webClient.BaseAddress = $"https://yfapi.net/v7/finance/options/{symbol}";
			webClient.Headers.Add("accept: application/json");
			webClient.Headers.Add("X-API-KEY: npF3vHM9Ga5sBvRIT9tHd8chMYxUdwzV7I7WXDMZ");

			string response = webClient.DownloadString(webClient.BaseAddress);
			dynamic obj = JsonConvert.DeserializeObject(response);
			var result = obj.optionChain.result;
			if (result.Count == 0)
				return false;
			return true;
		}
		#endregion
	}
}
