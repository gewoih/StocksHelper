using StocksHelper.Models.Base;
using System;

namespace StocksHelper.Models
{
	public class StockQuote : Entity
	{
		public int StockId { get; set; }

		private Stock _Stock;
		public Stock Stock
		{
			get => _Stock;
			set => Set(ref _Stock, value);
		}

		private DateTime _DateTime;
		public DateTime DateTime
		{
			get => _DateTime;
			set => Set(ref _DateTime, value);
		}

		private double _ClosePrice;
		public double ClosePrice
		{
			get => _ClosePrice;
			set => Set(ref _ClosePrice, value);
		}
	}
}
