using StocksHelper.Models.Base;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace StocksHelper.Repositories.Base
{
	public interface IRepository<T> where T : Entity
	{
		IQueryable<T> GetAll();
		T GetById(int id);
		T Create(T entity);
		IEnumerable<T> Create(IEnumerable<T> entities);
		void Update(T entity);
		void Delete(int id);
	}
}
