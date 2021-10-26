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

namespace StocksHelper.ViewModels
{
	public class StocksViewModel : BaseViewModel
	{
		#region Constructor
		public StocksViewModel(User User)
		{
			this.User = User;
		}
		#endregion

		#region Properties
		private User _User;
		public User User
		{
			get => _User;
			set => Set(ref _User, value);
		}
		#endregion
	}
}
