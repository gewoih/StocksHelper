using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories.Base;
using System.Linq;

namespace StocksHelper.Repositories
{
	public class StocksRepository : BaseRepository<Stock>
	{
		public StocksRepository(BaseDataContext dbContext) : base(dbContext) { }

		public override IQueryable<Stock> GetAll()
		{
			return base.GetAll().Include(s => s.StockQuotes).Include(s => s.Users);
		}

		public override Stock GetById(int id)
		{
			return _dbContext.Set<Stock>().Include(s => s.StockQuotes).FirstOrDefault(s => s.Id == id);
		}
	}
}
