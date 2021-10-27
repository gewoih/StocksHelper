using Microsoft.EntityFrameworkCore;
using StocksHelper.Models;
using System;
using System.Linq;

namespace StocksHelper.DataContext
{
	public class BaseDataContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Stock> Stocks { get; set; }
		public DbSet<StockQuotes> StocksQuotes { get; set; }

		public BaseDataContext()
		{
			Database.EnsureDeleted();
			Database.EnsureCreated();

			var s1 = new Stock { Symbol = "AAPL", Name = "Apple Inc." };
			var s2 = new Stock { Symbol = "SBERP.ME", Name = "Сбербанк п." };
			this.Stocks.AddRange(s1, s2);

			var u1 = new User { Username = "nranenko", Password = "123" };
			this.Users.AddRange(u1);

			u1.Stocks.Add(s1);
			u1.Stocks.Add(s2);

			SaveChanges();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseMySql(
							"server=server71.hosting.reg.ru;port=3306;user=u1507549_default;password=8fCFB6jH9x4D4ovv;database=u1507549_stockshelperdb;",
							new MySqlServerVersion(new Version(5, 7, 27)));
		}
	}
}
