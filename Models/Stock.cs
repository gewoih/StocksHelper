using StocksHelper.DataContext;
using StocksHelper.Models.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

		private ICollection<StockQuote> _StockQuotes = new ObservableCollection<StockQuote>();
		public ICollection<StockQuote> StockQuotes
		{
			get => _StockQuotes;
			set => Set(ref _StockQuotes, value);
		}

		private ICollection<User> _Users = new ObservableCollection<User>();
		public ICollection<User> Users
		{
			get => _Users;
			set => Set(ref _Users, value);
		}
		
		public DateTime GetDateTimeLastQuote()
		{
			if (this.StockQuotes.Count == 0)
				return DateTime.MinValue;

			return this.StockQuotes.ToList().Max(sq => sq.DateTime);
		}
	}
}
