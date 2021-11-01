using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories.Base;
using System.Linq;

namespace StocksHelper.Repositories
{
	public class StocksQuotesRepository : BaseRepository<StockQuote>
	{
		public StocksQuotesRepository(BaseDataContext dbContext) : base(dbContext) { }

		public override IQueryable<StockQuote> GetAll()
		{
			return base.GetAll().Include(s => s.Stock);
		}
	}
}
