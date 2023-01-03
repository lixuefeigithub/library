using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest.TestDatabase
{
    public class CountryOrRegion
    {
        [Key]
        public int CountryOrRegionId { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }
    }
}
