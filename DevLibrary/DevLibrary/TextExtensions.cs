using System;
using System.Globalization;

namespace DevLibrary
{
    public static class TextExtensions
    {
        /// <summary>
        /// Two string equals in SQL Server collation SQL_Latin1_General_CP1_CI_AI
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <returns></returns>
        public static bool Equals_Collation_SQL_Latin1_General_CP1_CI_AI(this string source1, string source2)
        {
            if (source1 == null && source2 == null)
            {
                return true;
            }

            if (source1 == null && source2 != null)
            {
                return false;
            }

            if (source1 != null && source2 == null)
            {
                return false;
            }

            //our db collation is "SQL_Latin1_General_CP1_CI_AI"

            /*
             * SELECT 'SQL_Latin1_General_CP1_CI_AI' AS 'Collation',
             * COLLATIONPROPERTY('SQL_Latin1_General_CP1_CI_AI', 'CodePage') AS 'CodePage', 
             * COLLATIONPROPERTY('SQL_Latin1_General_CP1_CI_AI', 'LCID') AS 'LCID',
             * COLLATIONPROPERTY('SQL_Latin1_General_CP1_CI_AI', 'ComparisonStyle') AS 'ComparisonStyle', 
             * COLLATIONPROPERTY('SQL_Latin1_General_CP1_CI_AI', 'Version') AS 'Version'
             * 
             * Result: Code Page = 1252, LCID = 1033, ComparisonStyle = 196611
             * 
             * Unicode Comparison Style Number 196611 is 'General Unicode', with:
             * 'Case Insensitive', 
             * 'Accent Insensitive', 
             * 'Width Insensitive' 
             * 'Kana Insensitive'
             */

            //LCID = 1033
            CompareInfo compareInfo = CultureInfo.GetCultureInfo(1033).CompareInfo;

            //Unicode Comparison Style Number 196611 is 'General Unicode', with 'Case Insensitive', 'Accent Insensitive', 'Width Insensitive' and 'Kana Insensitive'
            var compareOptions = CompareOptions.IgnoreCase
                //Accent Insensitive
                | CompareOptions.IgnoreNonSpace
                //Width Insensitive
                | CompareOptions.IgnoreWidth
                //Kana Insensitive
                | CompareOptions.IgnoreKanaType;

            var hashCode1 = compareInfo.GetHashCode(source1, compareOptions);
            var hashCode2 = compareInfo.GetHashCode(source2, compareOptions);

            return hashCode1 == hashCode2;
        }
    }
}
