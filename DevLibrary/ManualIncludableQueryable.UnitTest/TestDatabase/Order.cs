using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class Order
    {
        public Order()
        {
            OrderProducts = new HashSet<OrderProduct>();
        }

        [Key]
        public int OrderId { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        [ForeignKey(nameof(ClientUser))]
        public int ClientUserId { get; set; }

        public virtual ClientUser ClientUser { get; set; }

        [ForeignKey(nameof(ClientUserPaymentMethod))]
        public int ClientUserPaymentMethodId { get; set; }

        public virtual ClientUserPaymentMethod ClientUserPaymentMethod { get; set; }

        [ForeignKey(nameof(BillingAddress))]
        public int BillingAddressId { get; set; }

        public virtual Address BillingAddress { get; set; }

        [ForeignKey(nameof(DeliveryAddress))]
        public int DeliveryAddressId { get; set; }

        public virtual Address DeliveryAddress { get; set; }

        public virtual ICollection<OrderProduct> OrderProducts { get; set; }
    }
}
