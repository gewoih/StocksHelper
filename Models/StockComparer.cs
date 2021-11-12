using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StocksHelper.Models
{
    public class StockComparer : IEqualityComparer<Stock>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Stock x, Stock y)
        {
            if (Object.ReferenceEquals(x, y)) return true;

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            return x.Id == y.Id;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Stock stock)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(stock, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashStockId = stock.Id.GetHashCode();

            //Calculate the hash code for the product.
            return hashStockId;
        }
    }
}
