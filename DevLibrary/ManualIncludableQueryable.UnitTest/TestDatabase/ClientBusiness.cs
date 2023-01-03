using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ClientBusiness
    {
        public ClientBusiness()
        {
            ClientUsers = new HashSet<ClientUser>();
        }

        [Key]
        public int ClientBusinessId { get; set; }

        public string ClientBusinessName { get; set; }

        public virtual ICollection<ClientUser> ClientUsers { get; set; }
    }
}
