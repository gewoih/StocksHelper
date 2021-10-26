using StocksHelper.Models.Base;
using System.Collections.ObjectModel;

namespace StocksHelper.Models
{
	public class Stock : Entity
	{
		private string _Symbol;
		public string Symbol
		{
			get => _Symbol;
			set => Set(ref _Symbol, value);
		}

		private string _Name;
		public string Name
		{
			get => _Name;
			set => Set(ref _Name, value);
		}

		private ObservableCollection<StockQuotes> _StockQuotes;
		public ObservableCollection<StockQuotes> StockQuotes
		{
			get => _StockQuotes;
			set => Set(ref _StockQuotes, value);
		}

		private ObservableCollection<User> _Users;
		public ObservableCollection<User> Users
		{
			get => _Users;
			set => Set(ref _Users, value);
		}
	}
}
