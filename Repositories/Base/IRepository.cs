using StocksHelper.Models.Base;
using System.Linq;

namespace StocksHelper.Repositories.Base
{
	public interface IRepository<T> where T : Entity
	{
		IQueryable<T> GetAll();
		T GetById(int id);
		void Create(T entity);
		void Update(T entity);
		void Delete(int id);
	}
}
