using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class ClientUserProfile
    {
        [Key]
        public int ClientUserProfileId { get; set; }

        [ForeignKey(nameof(ClientUser))]
        [Index(IsUnique = true, IsClustered = false)]
        public int ClientUserId { get; set; }

        public virtual ClientUser ClientUser { get; set; }

        public string Age { get; set; }

        public string Title { get; set; }
    }
}
