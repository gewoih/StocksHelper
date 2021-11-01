﻿using Microsoft.EntityFrameworkCore;
using StocksHelper.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace StocksHelper.DataContext
{
	public class BaseDataContext : DbContext
	{
		public DbSet<User> Users { get; set; }
		public DbSet<Stock> Stocks { get; set; }
		public DbSet<StockQuote> StocksQuotes { get; set; }

		public BaseDataContext()
		{
			/*Database.EnsureDeleted();
			Database.EnsureCreated();

			var u1 = new User { Username = "nranenko", Password = "123" };
			var u2 = new User { Username = "1", Password = "1" };
			this.Users.AddRange(u1, u2);

			SaveChanges();*/
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseMySql(ConfigurationManager.ConnectionStrings["StocksHelperDB"].ConnectionString, new MySqlServerVersion(new Version(5, 7, 27)));
			optionsBuilder.LogTo(message => System.Diagnostics.Debug.WriteLine(message));
		}
	}
}
