using StocksHelper.DataContext;
using StocksHelper.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace StocksHelper.Models
{
	public static class Logger
	{
		public static void CreateLog(LogRecord newLog, ObservableCollection<LogRecord> recordsToAdd)
		{
			Application.Current.Dispatcher.Invoke
			(
				new Action(() => recordsToAdd.Add(new LogRecordsRepository(new BaseDataContext()).Create(newLog))),
				DispatcherPriority.Normal
			);
		}
	}
}
