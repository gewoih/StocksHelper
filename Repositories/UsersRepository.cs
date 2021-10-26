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
	}
}
