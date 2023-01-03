using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class MerchantProvider
    {
        public MerchantProvider()
        {
            ProductMerchantProviders = new HashSet<ProductMerchantProvider>();
        }

        [Key]
        public int MerchantProviderId { get; set; }

        public string Name { get; set; }

        public virtual ICollection<ProductMerchantProvider> ProductMerchantProviders { get; set; }
    }
}
