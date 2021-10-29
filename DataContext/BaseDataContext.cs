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

			var u1 = new User { Username = "nranenko", Password = "123" };
			var u2 = new User { Username = "1", Password = "1" };
			this.Users.AddRange(u1, u2);

			SaveChanges();
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseMySql(
							"server=server71.hosting.reg.ru;port=3306;user=u1507549_default;password=8fCFB6jH9x4D4ovv;database=u1507549_stockshelperdb;",
							new MySqlServerVersion(new Version(5, 7, 27)));
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder
				.Entity<User>()
				.HasMany(e => e.Stocks)
				.WithMany(e => e.Users);

			modelBuilder
				.Entity<Stock>()
				.HasMany(e => e.Users)
				.WithMany(e => e.Stocks);
		}
	}
}
