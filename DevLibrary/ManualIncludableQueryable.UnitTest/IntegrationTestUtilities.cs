using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ManualIncludableQueryable.UnitTest
{
    public static class IntegrationTestUtilities
    {
        private static readonly Regex dbNameRegex = new Regex(@";Database=([a-zA-Z0-9\.]{1,});");
        public static string GetDbNameFromConnectionString(this string dbConnectionString, string defaultDbName = "Gotham.IntegrationTests")
        {
            if (dbConnectionString != null && dbNameRegex.IsMatch(dbConnectionString))
            {
                return dbNameRegex.Match(dbConnectionString).Groups[1].Value;
            }

            return defaultDbName;
        }

        public static string GetLast(this string source, int tailLength)
        {
            if (tailLength >= source.Length)
            {
                return source;
            }

            return source.Substring(source.Length - tailLength);
        }
    }
}
