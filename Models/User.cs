using StocksHelper.Models.Base;
using System.Collections.ObjectModel;

namespace StocksHelper.Models
{
	public class User : Entity
	{
		private string _Username;
		public string Username
		{
			get => _Username;
			set => Set(ref _Username, value);
		}

		private string _Password;
		public string Password
		{
			get => _Password;
			set => Set(ref _Password, value);
		}

		private string _TelegramId;
		public string TelegramId
		{
			get => _TelegramId;
			set => Set(ref _TelegramId, value);
		}

		private ObservableCollection<Stock> _Stocks = new ObservableCollection<Stock>();
		public ObservableCollection<Stock> Stocks
		{
			get => _Stocks;
			set => Set(ref _Stocks, value);
		}
	}
}
