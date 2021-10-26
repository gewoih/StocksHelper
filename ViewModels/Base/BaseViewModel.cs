using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StocksHelper.ViewModels.Base
{
	public abstract class BaseViewModel : INotifyPropertyChanged
	{
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
