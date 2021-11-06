using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using StocksHelper.Commands;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories;
using StocksHelper.Repositories.Base;
using StocksHelper.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StocksHelper.ViewModels
{
	public class StocksViewModel : BaseViewModel
	{
		#region Constructor
		public StocksViewModel(Stock stock)
		{
			this.SelectedStock = stock;

			this._StockQuotesRepository = new StocksQuotesRepository(new BaseDataContext());
			this.PaintChart = new RelayCommand(OnPaintChartExecuted, CanPaintChartExecute);

			this.PaintChart.Execute(null);
		}
		#endregion

		#region Properties
		private IRepository<StockQuote> _StockQuotesRepository;

		private Stock _SelectedStock;
		public Stock SelectedStock
		{
			get => _SelectedStock;
			set => Set(ref _SelectedStock, value);
		}

		private PlotModel _OxyModel;
		public PlotModel OxyModel
		{
			get => _OxyModel;
			set => Set(ref _OxyModel, value);
		}
		#endregion

		#region Commands
		public ICommand PaintChart { get; }
		private bool CanPaintChartExecute(object p) => this.SelectedStock != null;
		private void OnPaintChartExecuted(object p)
		{
			this.OxyModel = new PlotModel();
			List<DataPoint> quotes = new List<DataPoint>();
			LineSeries priceSeries = new AreaSeries();
			LineSeries SMA50 = new LineSeries { Color = OxyColors.Green };
			LineSeries SMA200 = new LineSeries { Color = OxyColors.Blue };
			LineSeries CCI = new LineSeries { Color = OxyColors.Black };

			this.OxyModel.Axes.Add(new DateTimeAxis() { Title = "Дата" });
			this.OxyModel.Axes.Add(new LinearAxis() { Title = "Цена за акцию" });

			this._StockQuotesRepository.GetAll().Where(s => s.Stock.Id == this.SelectedStock.Id).OrderBy(q => q.DateTime).ToList().ForEach(q => quotes.Add(new DataPoint(DateTimeAxis.ToDouble(q.DateTime), q.ClosePrice)));
			priceSeries.Points.AddRange(quotes);
			SMA50.Points.AddRange(this.CalculateSMA(quotes, 50));
			SMA200.Points.AddRange(this.CalculateSMA(quotes, 200));
			CCI.Points.AddRange(this.CalculateCCI(quotes, 50));

			this.OxyModel.Series.Add(priceSeries);
			this.OxyModel.Series.Add(SMA50);
			this.OxyModel.Series.Add(SMA200);
			this.OxyModel.Series.Add(CCI);
			this.OxyModel.InvalidatePlot(true);
		}
		#endregion

		#region Methods
		private List<DataPoint> CalculateSMA(List<DataPoint> quotes, int period)
		{
			List<DataPoint> resultPoints = new List<DataPoint>();
			for (int i = 0; i < quotes.Count; i++)
			{
				if (i >= period - 1)
				{
					//Сумма цен на акцию за период
					double periodQuotesSum = 0;
					for (int j = i - period + 1; j <= i; j++)
						periodQuotesSum += quotes[j].Y;
					resultPoints.Add(new DataPoint(quotes[i].X, periodQuotesSum / period));
				}
				else
					resultPoints.Add(new DataPoint(quotes[i].X, 0));
			}
			return resultPoints;
		}

		private List<DataPoint> CalculateCCI(List<DataPoint> quotes, int period)
		{
			List<DataPoint> resultPoints = new List<DataPoint>();
			for (int i = 0; i < quotes.Count; i++)
			{
				//Индекс для удобства
				int index = i + 1;
				if (index > period)
				{
					//Вычисляем среднее значение типичной цены
					double avgTp = 0;
					for (int p = index - period; p < index; p++)
					{
						DataPoint d = quotes[p];
						avgTp += d.Y;
					}
					avgTp /= period;

					//Вычисляем среднее значение отклонений цены
					double avgDv = 0;
					for (int p = index - period; p < index; p++)
					{
						DataPoint d = quotes[p];
						avgDv += Math.Abs(avgTp - d.Y);
					}
					avgDv /= period;

					//Добавляем новое значение CCI по формуле
					resultPoints.Add(new DataPoint(quotes[i].X, (quotes[i].Y - avgTp) / (0.015 * avgDv)));
				}
			}
			return resultPoints;
		}

		private List<DataPoint> CalculateRSI(List<DataPoint> quotes, int period)
		{
			return null;
		}
		#endregion
	}
}
