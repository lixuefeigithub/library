using EFCore3Library;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    public static class ManualIncludableQueryableExtensions
    {
        /// <summary>
        /// Include one-to-many navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNavigation> IncludeManyManually<TEntity, TNavigation>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = ManualIncludableQueryable<TEntity, TEntity, TNavigation>.CreateFirstOneToManyIncludeChainQuery(source,
                navigationPropertyPath,
                dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyManually<TEntity, TNewNavigation>(
            this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyManually<TEntity, TNewNavigation>(
            this IOrderedManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include many-to-one or one-to-one navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isInvokeDistinctInMemory"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNavigation> IncludeOneManually<TEntity, TNavigation>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = ManualIncludableQueryable<TEntity, TEntity, TNavigation>.CreateFirstManyToOneIncludeChainQuery(source,
                navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return includableQuery;
        }

        /// <summary>
        /// Include many-to-one or one-to-one navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isInvokeDistinctInMemory"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeOneManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return includableQuery;
        }

        /// <summary>
        /// Include many-to-one or one-to-one navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isInvokeDistinctInMemory"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeOneManually<TEntity, TNewNavigation>(
            this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return includableQuery;
        }

        /// <summary>
        /// Include many-to-one or one-to-one navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isInvokeDistinctInMemory"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeOneManually<TEntity, TNewNavigation>(
            this IOrderedManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations with a unique key
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNavigation> IncludeManyUniqueManually<TEntity, TNavigation>(this IQueryable<TEntity> source,
           Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TEntity : class
           where TNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = ManualIncludableQueryable<TEntity, TEntity, TNavigation>.CreateFirstOneToManyUniqueIncludeChainQuery(source,
                navigationPropertyPath,
                dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations with a unique key
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyUniqueManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations with a unique key
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyUniqueManually<TEntity, TNewNavigation>(
            this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations with a unique key
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IncludeManyUniqueManually<TEntity, TNewNavigation>(
           this IOrderedManualIncludableQueryable<TEntity> source,
           Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TEntity : class
           where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TLastNavigation, TNewNavigation> ThenIncludeManyManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TLastNavigation, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateOneToManyThenIncludeQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        /// <summary>
        /// Include many-to-one or one-to-one navigations
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isInvokeDistinctInMemory"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TLastNavigation, TNewNavigation> ThenIncludeOneManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TLastNavigation, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateManyToOneThenIncludeQuery<TNewNavigation>(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return includableQuery;
        }

        /// <summary>
        /// Include one-to-many navigations with a unique key
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TSecondLastNavigation"></typeparam>
        /// <typeparam name="TLastNavigation"></typeparam>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="source"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static ManualIncludableQueryable<TEntity, TLastNavigation, TNewNavigation> ThenIncludeManyUniqueManually<TEntity, TSecondLastNavigation, TLastNavigation, TNewNavigation>(
            this ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> source,
            Expression<Func<TLastNavigation, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TSecondLastNavigation : class
            where TLastNavigation : class
            where TNewNavigation : class
        {
            if (source == null)
            {
                return null;
            }

            var includableQuery = source.CreateOneToManyUniqueThenIncludeQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return includableQuery;
        }

        public static ManualIncludableQueryable<TEntity, TEntity, TEntity> CreateEmptyManualIncludeableQuery<TEntity>(this IQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                return null;
            }

            var query = ManualIncludableQueryable<TEntity, TEntity, TEntity>.CreateEmptyManualIncludableQueryable(source);

            return query;
        }

        public static List<TEntity> ToList<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entities = source.InvokeQueryToList();

            return entities;
        }

        public static TEntity[] ToArray<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entities = source.InvokeQueryToArray();

            return entities;
        }

        public static TEntity FirstOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryFirstOrDefault();

            return entity;
        }

        public static TEntity FirstOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
           where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryFirstOrDefault(predicate);

            return entity;
        }

        public static TEntity First<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryFirst();

            return entity;
        }

        public static TEntity First<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
           where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryFirst(predicate);

            return entity;
        }

        public static TEntity LastOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryLastOrDefault();

            return entity;
        }

        public static TEntity LastOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
           where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryLastOrDefault(predicate);

            return entity;
        }

        public static TEntity Last<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryLast();

            return entity;
        }

        public static TEntity Last<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
           where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQueryLast(predicate);

            return entity;
        }

        public static TEntity SingleOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQuerySingleOrDefault();

            return entity;
        }

        public static TEntity SingleOrDefault<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
           where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQuerySingleOrDefault(predicate);

            return entity;
        }

        public static TEntity Single<TEntity>(this IManualIncludableQueryable<TEntity> source)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQuerySingle();

            return entity;
        }

        public static TEntity Single<TEntity>(this IManualIncludableQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var entity = source.InvokeQuerySingle(predicate);

            return entity;
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IManualIncludableQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Aggregate(seed, func);
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IManualIncludableQueryable<TSource> source, TAccumulate seed, Expression<Func<TAccumulate, TSource, TAccumulate>> func, Expression<Func<TAccumulate, TResult>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Aggregate(seed, func, selector);
        }

        public static bool All<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().All(predicate);
        }

        public static bool Any<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Any();
        }

        public static bool Any<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
             where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Any(predicate);
        }

        public static decimal Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, double>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, long>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static decimal? Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double? Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double? Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static double? Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static float? Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static float Average<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, float>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Average(selector);
        }

        public static bool Contains<TSource>(this IManualIncludableQueryable<TSource> source, TSource item)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Contains(item);
        }

        public static bool Contains<TSource>(this IManualIncludableQueryable<TSource> source, TSource item, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Contains(item, comparer);
        }

        public static int Count<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Count();
        }

        public static int Count<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Count(predicate);
        }

        public static IManualIncludableQueryable<TSource> DefaultIfEmpty<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.DefaultIfEmpty();

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> DefaultIfEmpty<TSource>(this IManualIncludableQueryable<TSource> source, TSource defaultValue)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.DefaultIfEmpty(defaultValue);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Distinct<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Distinct();

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Distinct<TSource>(this IManualIncludableQueryable<TSource> source, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Distinct(comparer);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Except<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            var queryable = source1.GetQueryable();
            var newQueryable = queryable.Except(source2);

            return source1.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Except<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            var queryable = source1.GetQueryable();
            var newQueryable = queryable.Except(source2, comparer);

            return source1.CreateNewReplaceQueryable(newQueryable);
        }

        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector);
        }

        public static IQueryable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IEqualityComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, comparer);
        }

        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, elementSelector);
        }

        public static IQueryable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, IEqualityComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, elementSelector, comparer);
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, resultSelector);
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TKey, IEnumerable<TSource>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, resultSelector, comparer);
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, elementSelector, resultSelector);
        }

        public static IQueryable<TResult> GroupBy<TSource, TKey, TElement, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, Expression<Func<TSource, TElement>> elementSelector, Expression<Func<TKey, IEnumerable<TElement>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().GroupBy(keySelector, elementSelector, resultSelector, comparer);
        }

        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IManualIncludableQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector)
            where TOuter : class
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            return outer.GetQueryable().GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        public static IQueryable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(this IManualIncludableQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, IEnumerable<TInner>, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
            where TOuter : class
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            return outer.GetQueryable().GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        public static IQueryable<TSource> Intersect<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            return source1.GetQueryable().Intersect(source2);
        }

        public static IQueryable<TSource> Intersect<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            return source1.GetQueryable().Intersect(source2, comparer);
        }

        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IManualIncludableQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector)
            where TOuter : class
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            return outer.GetQueryable().Join(inner, outerKeySelector, innerKeySelector, resultSelector);
        }

        public static IQueryable<TResult> Join<TOuter, TInner, TKey, TResult>(this IManualIncludableQueryable<TOuter> outer, IEnumerable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter, TInner, TResult>> resultSelector, IEqualityComparer<TKey> comparer)
            where TOuter : class
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            return outer.GetQueryable().Join(inner, outerKeySelector, innerKeySelector, resultSelector, comparer);
        }

        public static long LongCount<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().LongCount();
        }

        public static long LongCount<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().LongCount(predicate);
        }

        public static TSource Max<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Max();
        }

        public static TResult Max<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Max(selector);
        }

        public static TSource Min<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Min();
        }

        public static TResult Min<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Min(selector);
        }

        public static IQueryable<TResult> OfType<TSource, TResult>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().OfType<TResult>();
        }

        public static IOrderedManualIncludableQueryable<TSource> OrderBy<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var query = source.GetQueryable();

            var orderedQuery = query.OrderBy(keySelector);

            return source.CreateNewOrderedQueryable(orderedQuery);
        }


        public static IOrderedManualIncludableQueryable<TSource> OrderBy<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var query = source.GetQueryable();

            var orderedQuery = query.OrderBy(keySelector, comparer);

            return source.CreateNewOrderedQueryable(orderedQuery);
        }

        public static IOrderedManualIncludableQueryable<TSource> OrderByDescending<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var query = source.GetQueryable();

            var orderedQuery = query.OrderByDescending(keySelector);

            return source.CreateNewOrderedQueryable(orderedQuery);
        }

        public static IOrderedManualIncludableQueryable<TSource> OrderByDescending<TSource, TKey>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var query = source.GetQueryable();

            var orderedQuery = query.OrderByDescending(keySelector, comparer);

            return source.CreateNewOrderedQueryable(orderedQuery);
        }

        public static IManualIncludableQueryable<TSource> Prepend<TSource>(this IManualIncludableQueryable<TSource> source, TSource element)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Prepend(element);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Reverse<TSource>(this IManualIncludableQueryable<TSource> source)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Reverse();

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IQueryable<TResult> Select<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Select(selector);
        }

        public static IQueryable<TResult> Select<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, TResult>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Select(selector);
        }

        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TResult>>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().SelectMany(selector);
        }

        public static IQueryable<TResult> SelectMany<TSource, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TResult>>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().SelectMany(selector);
        }

        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().SelectMany(collectionSelector, resultSelector);
        }

        public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, IEnumerable<TCollection>>> collectionSelector, Expression<Func<TSource, TCollection, TResult>> resultSelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().SelectMany(collectionSelector, resultSelector);
        }

        public static bool SequenceEqual<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            return source1.GetQueryable().SequenceEqual(source2);
        }

        public static bool SequenceEqual<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            return source1.GetQueryable().SequenceEqual(source2, comparer);
        }

        public static IManualIncludableQueryable<TSource> Skip<TSource>(this IManualIncludableQueryable<TSource> source, int count)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Skip(count);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> SkipLast<TSource>(this IManualIncludableQueryable<TSource> source, int count)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.SkipLast(count);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> SkipWhile<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.SkipWhile(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> SkipWhile<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.SkipWhile(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static decimal Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static double Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, double>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static int Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static long Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, long>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static decimal? Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static double? Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, double?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static int? Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static long? Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, long?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static float? Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, float?>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static float Sum<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, float>> selector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.GetQueryable().Sum(selector);
        }

        public static IManualIncludableQueryable<TSource> Take<TSource>(this IManualIncludableQueryable<TSource> source, int count)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Take(count);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> TakeLast<TSource>(this IManualIncludableQueryable<TSource> source, int count)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.TakeLast(count);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> TakeWhile<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.TakeWhile(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> TakeWhile<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.TakeWhile(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IOrderedManualIncludableQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var orderedQuery = source.GetOrderedQueryable();

            var newOrderedQuery = orderedQuery.ThenBy(keySelector);

            return source.CreateNewReplaceOrdredQueryable(newOrderedQuery);
        }

        public static IOrderedManualIncludableQueryable<TSource> ThenBy<TSource, TKey>(this IOrderedManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var orderedQuery = source.GetOrderedQueryable();

            var newOrderedQuery = orderedQuery.ThenBy(keySelector, comparer);

            return source.CreateNewReplaceOrdredQueryable(newOrderedQuery);
        }

        public static IOrderedManualIncludableQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var orderedQuery = source.GetOrderedQueryable();

            var newOrderedQuery = orderedQuery.ThenByDescending(keySelector);

            return source.CreateNewReplaceOrdredQueryable(newOrderedQuery);
        }

        public static IOrderedManualIncludableQueryable<TSource> ThenByDescending<TSource, TKey>(this IOrderedManualIncludableQueryable<TSource> source, Expression<Func<TSource, TKey>> keySelector, IComparer<TKey> comparer)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var orderedQuery = source.GetOrderedQueryable();

            var newOrderedQuery = orderedQuery.ThenByDescending(keySelector, comparer);

            return source.CreateNewReplaceOrdredQueryable(newOrderedQuery);
        }

        public static IManualIncludableQueryable<TSource> Union<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            var queryable = source1.GetQueryable();
            var newQueryable = queryable.Union(source2);

            return source1.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Union<TSource>(this IManualIncludableQueryable<TSource> source1, IEnumerable<TSource> source2, IEqualityComparer<TSource> comparer)
            where TSource : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            var queryable = source1.GetQueryable();
            var newQueryable = queryable.Union(source2, comparer);

            return source1.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Where<TSource>(this IManualIncludableQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Where(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IManualIncludableQueryable<TSource> Where<TSource>(this IManualIncludableQueryable<TSource> source, Expression<Func<TSource, int, bool>> predicate)
            where TSource : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var queryable = source.GetQueryable();
            var newQueryable = queryable.Where(predicate);

            return source.CreateNewReplaceQueryable(newQueryable);
        }

        public static IQueryable<TResult> Zip<TFirst, TSecond, TResult>(this IManualIncludableQueryable<TFirst> source1, IEnumerable<TSecond> source2, Expression<Func<TFirst, TSecond, TResult>> resultSelector)
             where TFirst : class
        {
            if (source1 == null)
            {
                throw new ArgumentNullException(nameof(source1));
            }

            return source1.GetQueryable().Zip(source2, resultSelector);
        }
    }
}
