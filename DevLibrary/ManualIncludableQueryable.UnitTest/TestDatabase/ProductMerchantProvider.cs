using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ProductMerchantProvider
    {
        [Key]
        public int ProductMerchantProviderId { get; set; }

        [Index(IsUnique = true)]
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public virtual Product Product { get; set; }

        [ForeignKey(nameof(MerchantProvider))]
        public int MerchantProviderId { get; set; }

        public virtual MerchantProvider MerchantProvider { get; set; }

    }
}
