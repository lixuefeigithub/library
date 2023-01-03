using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class Product
    {
        public Product()
        {
            ProductPrices = new HashSet<ProductPrice>();
            OrderProducts = new HashSet<OrderProduct>();
        }

        [Key]
        public int ProductId { get; set; }

        public string ProductCategory { get; set; }

        public string ProductName { get; set; }

        public string ProductDescription { get; set; }

        public int PriceRevision { get; set; }

        [ForeignKey(nameof(License))]
        public int LicenseId { get; set; }

        public virtual License License { get; set; }


        [ForeignKey(nameof(Image))]
        public int? ImageId { get; set; }

        public virtual BlobStorageItem Image { get; set; }

        [ForeignKey(nameof(ProductLogo))]
        public int? ProductLogoId { get; set; }

        public virtual BlobStorageItem ProductLogo { get; set; }

        public virtual ProductMerchantProvider ProductMerchantProvider { get; set; }

        public virtual ICollection<ProductPrice> ProductPrices { get; set; }

        public virtual ICollection<OrderProduct> OrderProducts { get; set; }


        [NotMapped]
        public decimal? CurrentPrice { get; set; }
    }
}
