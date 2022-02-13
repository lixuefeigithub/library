using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqLibrary
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Not translatable in dot net 5
        /// </summary>
        /// <typeparam name="TLeftQuery">
        /// The type of the elements of the query on the left side
        /// </typeparam>
        /// <typeparam name="TRightQuery">
        /// The type of the elements of the query on the right side
        /// </typeparam>
        /// <typeparam name="TKey">
        /// The type of the keys returned by the key selector functions.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the result elements.
        /// </typeparam>
        /// <param name="leftQuery">
        /// A query on the left side of a left join query transaction
        /// </param>
        /// <param name="rightQuery">
        /// A query on the right side of a left join query transaction
        /// Do not Add selector to inner query. just use the entity query.
        /// Like _dbContext.Users.AsQueryable() is good, _dbContext.Users.AsQueryable().Select(x => new {x.UserId, x.Name }) is bad
        /// The selectors in inner query cannot be translated, it's good enough to add all your selectors to resultSelector.
        /// </param>
        /// <param name="leftQueryKeySelector">
        /// A function to extract the join key from each element of the left sequence.
        /// </param>
        /// <param name="rightQueryKeySelector">
        /// A function to extract the join key from each element of the right sequence.
        /// </param>
        /// <param name="resultSelector">
        /// A function to create a result element from an element from the first sequence
        //     and a collection of matching elements from the second sequence.
        /// </param>
        /// <returns></returns>
        [Obsolete]
        public static IQueryable<TResult> LeftJoinDotNetCore22<TLeftQuery, TRightQuery, TKey, TResult>(
            this IQueryable<TLeftQuery> leftQuery,
            IQueryable<TRightQuery> rightQuery,
            Expression<Func<TLeftQuery, TKey>> leftQueryKeySelector,
            Expression<Func<TRightQuery, TKey>> rightQueryKeySelector,
            Expression<Func<TLeftQuery, TRightQuery, TResult>> resultSelector)
        {
            var groupJoinQuery = leftQuery
                .GroupJoin(rightQuery, leftQueryKeySelector, rightQueryKeySelector, (outerObj, inners) => new LeftJoinCollectionModel<TLeftQuery, TRightQuery>
                {
                    LeftOuter = outerObj,
                    RightInners = inners.DefaultIfEmpty()
                });

            //Old parameter left
            ParameterExpression paramResultSeletorLeft = resultSelector.Parameters.First();

            //New parameter left.Outer
            ParameterExpression paramSelectManyResultSeletorLeft = Expression.Parameter(typeof(LeftJoinCollectionModel<TLeftQuery, TRightQuery>), "x");
            MemberExpression paramSelectManyResultSeletorLeftOuter = Expression.Property(paramSelectManyResultSeletorLeft, nameof(LeftJoinCollectionModel<TLeftQuery, TRightQuery>.LeftOuter));

            var selectManyResultSelector = Expression.Lambda<Func<LeftJoinCollectionModel<TLeftQuery, TRightQuery>, TRightQuery, TResult>>(new LeftJoinReplacer(paramResultSeletorLeft, paramSelectManyResultSeletorLeftOuter).Visit(resultSelector.Body), paramSelectManyResultSeletorLeft, resultSelector.Parameters.Skip(1).First());

            return groupJoinQuery.SelectMany(x => x.RightInners, selectManyResultSelector);
        }

        /// <summary>
        /// Translatable to db query, please note that do not add selectors to right query
        /// </summary>
        /// <typeparam name="TLeftQuery">
        /// The type of the elements of the query on the left side
        /// </typeparam>
        /// <typeparam name="TRightQuery">
        /// The type of the elements of the query on the right side
        /// </typeparam>
        /// <typeparam name="TKey">
        /// The type of the keys returned by the key selector functions.
        /// </typeparam>
        /// <param name="leftQuery">
        /// A query on the left side of a left join query transaction
        /// </param>
        /// <param name="rightQuery">
        /// A query on the right side of a left join query transaction
        /// Do not Add selector to inner query. just use the entity query.
        /// Like _dbContext.Users.AsQueryable() is good, _dbContext.Users.AsQueryable().Select(x => new {x.UserId, x.Name }) is bad
        /// The selectors in inner query cannot be translated, it's good enough to add all your selectors to resultSelector.
        /// </param>
        /// <param name="leftQueryKeySelector">
        /// A function to extract the join key from each element of the left sequence.
        /// </param>
        /// <param name="rightQueryKeySelector">
        /// A function to extract the join key from each element of the right sequence.
        /// </param>
        /// <returns></returns>
        public static IQueryable<LeftJoinResult<TLeftQuery, TRightQuery>> LeftJoin<TLeftQuery, TRightQuery, TKey>(
            this IQueryable<TLeftQuery> leftQuery,
            IQueryable<TRightQuery> rightQuery,
            Expression<Func<TLeftQuery, TKey>> leftQueryKeySelector,
            Expression<Func<TRightQuery, TKey>> rightQueryKeySelector)
        {
            var groupJoinQuery = leftQuery
                .GroupJoin(rightQuery,
                    leftQueryKeySelector,
                    rightQueryKeySelector,
                    (outerObj, inners) => new LeftJoinCollectionModel<TLeftQuery, TRightQuery>
                    {
                        LeftOuter = outerObj,
                        RightInners = inners
                    })
                .SelectMany(s => s.RightInners.DefaultIfEmpty(),
                    (s, righInner) => new LeftJoinResult<TLeftQuery, TRightQuery>
                    {
                        LeftOuter = s.LeftOuter,
                        RightInner = righInner
                    });

            return groupJoinQuery;
        }

        public static IQueryable<TSource> Or<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> orCondition)
        {
            if (source == null)
            {
                return source;
            }

            if (source.Expression is MethodCallExpression qe && qe.Method.Name == "Where")
            {
                var we = (MethodCallExpression)source.Expression; // get the call to Where
                var wea1 = (UnaryExpression)we.Arguments[1]; // get the 2nd arg to Where (Quoted Lambda)
                var leftExpr = (Expression<Func<TSource, bool>>)wea1.Operand; // Extract the lambda from the QuoteExpression
                var newWhereClause = leftExpr.Or(orCondition);
                var newQuery = source.Provider.CreateQuery<TSource>(we.Arguments[0]).Where(newWhereClause);

                return newQuery;
            }

            return source.Where(orCondition);
        }

        public class LeftJoinResult<TLeftOuter, TRightInner>
        {
            public TLeftOuter LeftOuter { get; set; }
            public TRightInner RightInner { get; set; }
        }

        private class LeftJoinReplacer : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly Expression _replacement;

            public LeftJoinReplacer(ParameterExpression oldParam, Expression replacement)
            {
                _oldParam = oldParam;
                _replacement = replacement;
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == _oldParam)
                {
                    return _replacement;
                }

                return base.Visit(exp);
            }
        }

        private class LeftJoinCollectionModel<TOuter, TInner>
        {
            public TOuter LeftOuter { get; set; }
            public IEnumerable<TInner> RightInners { get; set; }
        }
    }
}
