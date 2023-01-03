using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ManualIncludableQueryable
{
    public static partial class QueryableExtensions
    {
        public static IManualIncludableQueryable<TSource> TakeSafeQueryable<TSource>(this IManualIncludableQueryable<TSource> source, int? truncateSize)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();

            var newQueryable = queryable;

            if (truncateSize.HasValue)
            {
                newQueryable = newQueryable.Take(truncateSize.Value);
            }

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static int PageSize { get; set; } = 10;

        public static IManualIncludableQueryable<T> Page<T>(this IManualIncludableQueryable<T> source, int? page, int? pageSize = null)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (page == null)
            {
                return source;
            }

            var newQueryable = source.GetQueryable().Page(page, pageSize);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        private static IQueryable<T> Page<T>(this IQueryable<T> source, int? page, int? pageSize = null)
        {
            if (page == null)
            {
                return source;
            }

            pageSize = pageSize ?? PageSize;

            return source
                .Skip((page.Value - 1) * pageSize.Value)
                .Take(pageSize.Value);
        }
    }
}
