using EFCoreLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ProductPrice
    {
        [Key]
        public int ProductPriceId { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }

        public int Revsision { get; set; }

        public DateTimeOffset RevisionTimestamp { get; set; }

        [DecimalPrecision(2, 18)]
        public decimal Price { get; set; }
    }
}
