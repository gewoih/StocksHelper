using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models.Base;
using System.Linq;

namespace StocksHelper.Repositories.Base
{
	public class BaseRepository<T> : IRepository<T> where T : Entity, new()
	{
		protected readonly BaseDataContext _dbContext;
		protected DbSet<T> Set;

		public BaseRepository(BaseDataContext dbContext)
		{
			this._dbContext = dbContext;
			this.Set = dbContext.Set<T>();
		}

		public virtual IQueryable<T> GetAll()
		{
			return this.Set;
		}

		public virtual T GetById(int id)
		{
			return this.Set.Find(id);
		}

		public virtual void Create(T entity)
		{
			this.Set.Add(entity);
			this._dbContext.SaveChanges();
		}

		public virtual void Update(T entity)
		{
			this.Set.Attach(entity);
			this._dbContext.Entry(entity).State = EntityState.Modified;
			this._dbContext.SaveChanges();
		}

		public void Delete(int id)
		{
			var entity = Set.Find(id);

			if (this._dbContext.Entry(entity).State == EntityState.Detached)
				this.Set.Attach(entity);
			this.Set.Remove(entity);

			this._dbContext.SaveChanges();
		}
	}
}
