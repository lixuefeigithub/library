using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinFormLibrary
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class SortFieldAttribute : Attribute
    {
        public string SortFieldName { get; set; }
    }
}
