using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class PaymentMethod
    {
        public PaymentMethod()
        {
            ClientUserPaymentMethods = new HashSet<ClientUserPaymentMethod>();
        }

        [Key]
        public int PaymentMethodId { get; set; }


        [ForeignKey(nameof(PaymentMethodPicture))]
        public int PaymentMethodPictureId { get; set; }

        public virtual BlobStorageItem PaymentMethodPicture { get; set; }

        [InverseProperty(nameof(ClientUserPaymentMethod.PaymentMethod))]
        public virtual ICollection<ClientUserPaymentMethod> ClientUserPaymentMethods { get; set; }
    }
}
