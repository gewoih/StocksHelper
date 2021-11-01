using StocksHelper.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StocksHelper.Models
{
	public class UserStock : Entity
	{
		public int UserId { get; set; }
		public User User { get; set; }

		public int StockId { get; set; }
		public Stock Stock { get; set; }
	}
}
