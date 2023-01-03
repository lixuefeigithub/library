using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class Address
    {
        public Address()
        {
            OrderBillingAddressInverseProperties = new HashSet<Order>();
            OrderDeliveryAddressInverseProperties = new HashSet<Order>();
        }

        [Key]
        public int AddressId { get; set; }

        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        public string City { get; set; }

        public string ZipCode { get; set; }

        [ForeignKey(nameof(CountryOrRegion))]
        public int CountryOrRegionId { get; set; }

        public virtual CountryOrRegion CountryOrRegion { get; set; }

        [InverseProperty(nameof(Order.BillingAddress))]
        public virtual ICollection<Order> OrderBillingAddressInverseProperties { get; set; }

        [InverseProperty(nameof(Order.DeliveryAddress))]
        public virtual ICollection<Order> OrderDeliveryAddressInverseProperties { get; set; }
    }
}
