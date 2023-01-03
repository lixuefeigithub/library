using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ClientUserPaymentMethod
    {
        public ClientUserPaymentMethod()
        {
            Orders = new HashSet<Order>();
        }

        [Key]
        public int ClientUserPaymentMethodId { get; set; }

        [ForeignKey(nameof(ClientUser))]
        public int ClientUserId { get; set; }

        public virtual ClientUser ClientUser { get; set; }


        [ForeignKey(nameof(PaymentMethod))]
        public int? PaymentMethodId { get; set; }

        public virtual PaymentMethod PaymentMethod { get; set; }

        [InverseProperty(nameof(Order.ClientUserPaymentMethod))]
        public virtual ICollection<Order> Orders { get; set; }
    }
}
