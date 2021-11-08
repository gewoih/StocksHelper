using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models.Base;
using System.Collections.Generic;
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

		public virtual T GetById(int id)
		{
			return _dbContext.Set<T>().Find(id);
		}

		public virtual T Create(T entity)
		{
			_dbContext.Entry(entity).State = EntityState.Added;
			_dbContext.SaveChanges();

			return GetById(entity.Id);
		}

		public virtual IEnumerable<T> Create(IEnumerable<T> entities)
		{
			_dbContext.Set<T>().AddRange(entities);
			_dbContext.SaveChanges();

			return entities;
		}

		public void Update(T entity)
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
