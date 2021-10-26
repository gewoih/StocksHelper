using StocksHelper.Models;
using StocksHelper.ViewModels.Base;
using StocksHelper.Views;
using System.Windows.Controls;

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{

		}
		#endregion

		#region Properties
		private UserControl _mainContentControl = new AuthenticationView();
		public UserControl MainContentControl
		{
			get => _mainContentControl;
			set => Set(ref _mainContentControl, value);
		}

		private User _User;
		public User User
		{
			get => _User;
			set => Set(ref _User, value);
		}
		#endregion

		#region Commands
		#endregion
	}
}
