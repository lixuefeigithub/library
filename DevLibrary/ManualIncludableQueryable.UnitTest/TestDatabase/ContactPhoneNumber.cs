using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ContactPhoneNumber
    {
        [Key]
        public int ContactPhoneNumberId { get; set; }

        public string PhoneNumber { get; set; }

        public string NickName { get; set; }


        [ForeignKey(nameof(ClientUser))]
        public int? ClientUserId { get; set; }

        public virtual ClientUser ClientUser { get; set; }
    }
}
