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
			return base.GetAll().Include(u => u.Stocks).ThenInclude(s => s.StockQuotes);
		}

		public void AddStock(int userId, int stockId)
		{
			base._dbContext.Users.Find(userId).Stocks.Add(base._dbContext.Stocks.Find(stockId));
			base._dbContext.SaveChanges();
		}

		public void RemoveStock(int userId, int stockId)
		{
			User user = this._dbContext.Users.FirstOrDefault(s => s.Id == userId);
			Stock stock = this._dbContext.Stocks.FirstOrDefault(s => s.Id == stockId);
			this._dbContext.Entry(user).Collection("Stocks").Load();
			user.Stocks.Remove(stock);
			this._dbContext.SaveChanges();
		}
	}
}