using Microsoft.EntityFrameworkCore;
using StocksHelper.Models;
using System;

namespace StocksHelper.DataContext
{
	public class BaseDataContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Stock> Stocks { get; set; }
		public DbSet<StockQuotes> StocksQuotes { get; set; }

		public BaseDataContext()
		{
			Database.EnsureCreated();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseMySql(
							"server=server71.hosting.reg.ru;port=3306;user=u1507549_default;password=8fCFB6jH9x4D4ovv;database=u1507549_stockshelperdb;",
							new MySqlServerVersion(new Version(5, 7, 27)));
		}
	}
}
