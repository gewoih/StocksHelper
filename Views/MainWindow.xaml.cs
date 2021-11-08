using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace StocksHelper.Views
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			this.dataGrid.Items.SortDescriptions.Add(new SortDescription("DateTime", ListSortDirection.Descending));
		}
	}
}
