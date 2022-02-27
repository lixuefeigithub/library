/*
 * Sql server built-in functions or custom functions translations map
 * Reference: https://stackoverflow.com/questions/52529454/sqlfunctions-datepart-equivalent-in-ef-core
 */

using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace EFCore3Library
{
    public partial class DemoEfCore3DbContext : DbContext
    {
        public int? DatePart(string datePartArg, DateTime? date) => throw new NotImplementedException();

        public string Right(string source, int legnth) => throw new NotImplementedException();

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            var methodInfoDatePart = typeof(DbContext).GetRuntimeMethod(nameof(DatePart), new[] { typeof(string), typeof(DateTime?) });

            var methodInfoRight = typeof(DbContext).GetRuntimeMethod(nameof(Right), new[] { typeof(string), typeof(int) });

            modelBuilder
                .HasDbFunction(methodInfoDatePart)
                .HasTranslation(args => new SqlFunctionExpression(functionName: nameof(DatePart),
                    arguments: new SqlExpression[]
                        {
                        new SqlFragmentExpression(args.ToArray()[0].ToString()),
                        args.ToArray()[1]
                        },
                        nullable: true,
                        argumentsPropagateNullability: new bool[] { false, true },
                        type: typeof(int?),
                        typeMapping: null));

            modelBuilder
                .HasDbFunction(methodInfoRight)
                .HasTranslation(args => new SqlFunctionExpression(functionName: nameof(Right),
                    arguments: new SqlExpression[]
                        {
                        args.ToArray()[0],
                        args.ToArray()[1]
                        },
                        nullable: true,
                        argumentsPropagateNullability: new bool[] { true, false },
                        type: typeof(string),
                        typeMapping: null));
        }
    }
}
