using StocksHelper.Commands;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories;
using StocksHelper.Repositories.Base;
using StocksHelper.ViewModels.Base;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace StocksHelper.ViewModels
{
	public class AuthenticationViewModel : BaseViewModel
	{
		private MainWindowViewModel _MainVM;

		#region Constructor
		public AuthenticationViewModel(MainWindowViewModel MainVM)
		{
			this._MainVM = MainVM;
			this._UsersRepository = new UsersRepository(new BaseDataContext());
			this.AuthUserCommand = new RelayCommand(OnAuthUserCommandExecuted, CanAuthUserCommandExecute);
		}
		#endregion

		#region Properties
		private IRepository<User> _UsersRepository;

		private string _Username;
		public string Username
		{
			get => _Username;
			set => Set(ref _Username, value);
		}

		private string _Password;
		public string Password
		{
			get => _Password;
			set => Set(ref _Password, value);
		}
		#endregion

		#region Commands
		public ICommand AuthUserCommand { get; }
		private bool CanAuthUserCommandExecute(object p) => true;
		private void OnAuthUserCommandExecuted(object p)
		{
			User FindedUser = this._UsersRepository.GetAll().FirstOrDefault(u => u.Username == this.Username);
			if (FindedUser != null)
			{
				if (FindedUser.Password == this.CalculateHash(p.ToString() + FindedUser.Id))
				{
					this._MainVM.LoggedInUser = FindedUser;
					MessageBox.Show("Вы успешно авторизованы.");
				}
				else
					MessageBox.Show("Введен неверный пароль.");
			}
			else
				MessageBox.Show("Пользователя с таким логином не существует.");
		}
		#endregion

		#region Methods
		private string CalculateHash(string text)
		{
			byte[] hashBytes = Encoding.UTF8.GetBytes(text);

			HashAlgorithm algorithm = new SHA256Managed();
			byte[] hash = algorithm.ComputeHash(hashBytes);

			return Convert.ToBase64String(hash);
		}
		#endregion
	}
}
