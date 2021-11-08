using Microsoft.EntityFrameworkCore;
using StocksHelper.DataContext;
using StocksHelper.Models;
using StocksHelper.Repositories.Base;
using System.Linq;

namespace StocksHelper.Repositories
{
	public class LogRecordsRepository : BaseRepository<LogRecord>
	{
		public LogRecordsRepository(BaseDataContext dbContext) : base(dbContext) { }

		public override IQueryable<LogRecord> GetAll()
		{
			return base.GetAll().Include(lr => lr.FromUser).Include(lr => lr.ToUser);
		}
	}
}
