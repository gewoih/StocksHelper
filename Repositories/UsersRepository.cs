using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories.Base;
using System.Linq;

namespace StocksHelper.Repositories
{
	public class UsersRepository : BaseRepository<User>
	{
		public UsersRepository(BaseDataContext dbContext) : base(dbContext) { }

		public override IQueryable<User> GetAll()
		{
			return base.GetAll().Include(u => u.Stocks);
		}

		public void AddStock(User user, Stock stock)
		{
			base._dbContext.Set<User>().Find(user.Id).Stocks.Add(stock);
			base._dbContext.SaveChanges();
		}

		public void RemoveStock(User user, Stock stock)
		{
			base._dbContext.Set<User>().Find(user.Id).Stocks.Remove(stock);
			base._dbContext.SaveChanges();
		}
	}
}
