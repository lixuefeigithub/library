/*
 * Sql server built-in functions or custom functions translations map
 * Reference: https://stackoverflow.com/questions/52529454/sqlfunctions-datepart-equivalent-in-ef-core
 */

using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Expressions;

namespace EFCore2._2Library
{
    public partial class DemoEfCoreDbContext : DbContext
    {
        public int? DatePart(string datePartArg, DateTime? date) => throw new NotImplementedException();

        public string Right(string source, int legnth) => throw new NotImplementedException();

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            var methodInfoDatePart = typeof(DemoEfCoreDbContext).GetRuntimeMethod(nameof(DatePart), new[] { typeof(string), typeof(DateTime?) });

            var methodInfoRight = typeof(DemoEfCoreDbContext).GetRuntimeMethod(nameof(Right), new[] { typeof(string), typeof(int) });

            modelBuilder
                .HasDbFunction(methodInfoDatePart)
                .HasTranslation(args => new SqlFunctionExpression(nameof(DatePart), typeof(int?), new[]
                        {
                        new SqlFragmentExpression(args.ToArray()[0].ToString()),
                        args.ToArray()[1]
                        }));

            modelBuilder
                .HasDbFunction(methodInfoRight)
                .HasTranslation(args => new SqlFunctionExpression(nameof(Right), typeof(string), new[]
                        {
                        args.ToArray()[0],
                        args.ToArray()[1]
                        }));
        }
    }
}
