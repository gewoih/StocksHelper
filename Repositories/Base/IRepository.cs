using StocksHelper.Models.Base;
using System.Linq;

namespace StocksHelper.Repositories.Base
{
	public interface IRepository<T> where T : Entity
	{
		IQueryable<T> GetAll();

		T GetById(int id);

		T Create(T entity);

		void Update(int id, T entity);

		void Delete(int id);
	}
}
