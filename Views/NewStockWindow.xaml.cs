using StocksHelper.ViewModels.Base;
using System.Windows;

namespace StocksHelper.Views
{
	/// <summary>
	/// Логика взаимодействия для NewStockView.xaml
	/// </summary>
	public partial class NewStockWindow : Window
	{
		public NewStockWindow(BaseViewModel ViewModel)
		{
			InitializeComponent();

			this.DataContext = ViewModel;
		}
	}
}
