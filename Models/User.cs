using StocksHelper.Models.Base;
using System.Collections.Generic;
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

		private ICollection<Stock> _Stocks = new ObservableCollection<Stock>();
		public ICollection<Stock> Stocks
		{
			get => _Stocks;
			set => Set(ref _Stocks, value);
		}

		private bool _IsActive;
		public bool IsActive
		{
			get => _IsActive;
			set => Set(ref _IsActive, value);
		}
	}
}
