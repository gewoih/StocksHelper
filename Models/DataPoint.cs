namespace StocksHelper.Models
{
	public class DataPoint
	{
		public double X { get; set; }
		public double Y { get; set; }

		public DataPoint(double x, double y)
		{
			this.X = x;
			this.Y = y;
		}
	}
}
