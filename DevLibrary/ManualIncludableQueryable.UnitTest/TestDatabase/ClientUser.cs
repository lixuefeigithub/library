using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ClientUser
    {
        public ClientUser()
        {
            Orders = new HashSet<Order>();
            ContactEmails = new HashSet<ContactEmail>();
            ContactPhoneNumbers = new HashSet<ContactPhoneNumber>();
            ClientUserPaymentMethods = new HashSet<ClientUserPaymentMethod>();
        }

        [Key]
        public int ClientUserId { get; set; }

        [ForeignKey(nameof(ClientBusiness))]
        public int? ClientBusinessId { get; set; }

        public virtual ClientBusiness ClientBusiness { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        [ForeignKey(nameof(IdentityCardBlobStorageItem))]
        public int IdentityCardBlobStorageItemId { get; set; }

        public virtual BlobStorageItem IdentityCardBlobStorageItem { get; set; }

        public virtual ClientUserProfile ClientUserProfile { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<ContactEmail> ContactEmails { get; set; }
        public virtual ICollection<ContactPhoneNumber> ContactPhoneNumbers { get; set; }
        public virtual ICollection<ClientUserPaymentMethod> ClientUserPaymentMethods { get; set; }
    }
}
