using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class License
    {
        public License()
        {
            Products = new HashSet<Product>();
        }

        [Key]
        public int LicenseId { get; set; }

        public DateTime IssuedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public virtual ICollection<Product> Products { get; set; }
    }
}
