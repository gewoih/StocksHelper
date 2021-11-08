using StocksHelper.Models.Base;
using System;

namespace StocksHelper.Models
{
	public class LogRecord : Entity
	{
		private User _FromUser;
		public User FromUser
		{
			get => _FromUser;
			set => Set(ref _FromUser, value);
		}

		private User _ToUser;
		public User ToUser
		{
			get => _ToUser;
			set => Set(ref _ToUser, value);
		}

		private DateTime _DateTime;
		public DateTime DateTime
		{
			get => _DateTime;
			set => Set(ref _DateTime, value);
		}

		private string _Message;
		public string Message
		{
			get => _Message;
			set => Set(ref _Message, value);
		}
	}
}
