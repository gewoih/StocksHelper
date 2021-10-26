using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models.Base;
using System.Linq;

namespace StocksHelper.Repositories.Base
{
	public class BaseRepository<T> : IRepository<T> where T : Entity, new()
	{
		protected readonly BaseDataContext _dbContext;

		public BaseRepository(BaseDataContext dbContext)
		{
			this._dbContext = dbContext;
		}

		public virtual IQueryable<T> GetAll()
		{
			return _dbContext.Set<T>();
		}

		public T GetById(int id)
		{
			return GetAll().FirstOrDefault(item => item.Id == id);
		}

		public virtual T Create(T entity)
		{
			_dbContext.Entry(entity).State = EntityState.Added;
			_dbContext.SaveChanges();

			return GetById(entity.Id);
		}

		public void Update(int id, T entity)
		{
			_dbContext.Entry(entity).State = EntityState.Modified;
			_dbContext.SaveChanges();
		}

		public void Delete(int id)
		{
			var entity = GetById(id);
			_dbContext.Entry(entity).State = EntityState.Deleted;

			_dbContext.SaveChanges();
		}
	}
}
