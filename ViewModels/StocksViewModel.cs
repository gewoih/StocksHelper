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
			LineSeries series = new AreaSeries();

			this.OxyModel.Axes.Add(new DateTimeAxis() { Title = "Дата" });
			this.OxyModel.Axes.Add(new LinearAxis() { Title = "Цена за акцию" });

			List<StockQuote> quotes = this._StockQuotesRepository.GetAll().Where(s => s.Stock.Id == this.SelectedStock.Id).OrderBy(q => q.DateTime).ToList();
			quotes.ForEach(q => series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(q.DateTime), q.ClosePrice)));

			this.OxyModel.Series.Add(series);
			this.OxyModel.InvalidatePlot(true);
		}
		#endregion
	}
}
