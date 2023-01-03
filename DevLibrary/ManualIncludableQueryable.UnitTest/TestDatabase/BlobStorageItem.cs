using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class BlobStorageItem
    {
        public BlobStorageItem()
        {
            ClientUsers = new HashSet<ClientUser>();
        }

        [Key]
        public int BlobStorageItemId { get; set; }

        public string BlobName { get; set; }

        //reverse property
        public virtual ICollection<ClientUser> ClientUsers { get; set; }
    }
}
