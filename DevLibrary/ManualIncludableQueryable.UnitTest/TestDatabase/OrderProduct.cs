using EFCoreLibrary;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    [PrimaryKey(nameof(OrderProduct.OrderId), nameof(OrderProduct.ProductId))]
    public class OrderProduct
    {
        //[Key, Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey(nameof(Order))]
        public int OrderId { get; set; }

        public virtual Order Order { get; set; }

        //[Key, Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }

        [DecimalPrecision(2, 18)]
        public decimal Price { get; set; }
    }
}
