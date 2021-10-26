using StocksHelper.Models;
using StocksHelper.ViewModels.Base;

namespace StocksHelper.ViewModels
{
	public class MainWindowViewModel : BaseViewModel
	{
		#region Constructor
		public MainWindowViewModel()
		{
			this.MainContentControl = new AuthenticationViewModel(ref this._LoggedInUser);
		}
		#endregion

		#region Properties
		private object _mainContentControl;
		public object MainContentControl
		{
			get => _mainContentControl;
			set => Set(ref _mainContentControl, value);
		}

		private User _LoggedInUser;
		public User LoggedInUser
		{
			get => _LoggedInUser;
			set
			{
				Set(ref _LoggedInUser, value);
				this.MainContentControl = new StocksViewModel(this.LoggedInUser);
			}
		}
		#endregion

		#region Commands
		#endregion
	}
}
