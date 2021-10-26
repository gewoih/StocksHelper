using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StocksHelper.Models.Base
{
	public abstract class Entity : INotifyPropertyChanged
	{
		private int _Id;
		public int Id
		{
			get => _Id;
			set => Set(ref _Id, value);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (Equals(field, value))
				return false;

			field = value;
			OnPropertyChanged(propertyName);

			return true;
		}
	}
}
