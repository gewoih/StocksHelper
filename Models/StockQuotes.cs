using StocksHelper.Models.Base;

namespace StocksHelper.Models
{
	public class StockQuotes : Entity
	{
		private Stock _Stock;
		public Stock Stock
		{
			get => _Stock;
			set => Set(ref _Stock, value);
		}

		private double _Timestamp;
		public double Timestamp
		{
			get => _Timestamp;
			set => Set(ref _Timestamp, value);
		}

		private double _ClosePrice;
		public double ClosePrice
		{
			get => _ClosePrice;
			set => Set(ref _ClosePrice, value);
		}
	}
}
