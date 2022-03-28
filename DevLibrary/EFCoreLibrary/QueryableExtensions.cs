using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace System.Linq
{
    public static class QueryableExtensions
    {
        public static bool GetIsTracking<TEntity>(this IQueryable<TEntity> source, DbContext dbContext)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var isTrackingByDefault = dbContext.ChangeTracker.QueryTrackingBehavior == QueryTrackingBehavior.TrackAll;

            var expressionIsTrackingGetter = new ExpressionIsTrackingGetter(isTrackingByDefault);

            return expressionIsTrackingGetter.IsTracking(source);
        }

        public static bool GetIsOrderApplied<TEntity>(this IQueryable<TEntity> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var expressionIsOrderAppliedGetter = new ExpressionIsOrderAppliedGetter();

            return expressionIsOrderAppliedGetter.IsOrderApplied(source);
        }

        public static bool GetIsTakeApplied<TEntity>(this IQueryable<TEntity> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var expressionIsTakeAppliedGetter = new ExpressionIsTakeAppliedGetter();

            return expressionIsTakeAppliedGetter.IsTakeApplied(source);
        }

        public static bool IsMissingOrderBeforeTakeOrSkip<TEntity>(this IQueryable<TEntity> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var expressionOrderBeforeTakeValidator = new ExpressionOrderBeforeTakeOrSkipValidator();

            return expressionOrderBeforeTakeValidator.IsMissingOrderBeforeTake(source);
        }

        public class ExpressionIsTrackingGetter : ExpressionVisitor
        {
            private bool _hasAsNoTracking = false;
            private bool _hasAsTracking = false;

            private bool _DefaultIsTracking = false;

            public ExpressionIsTrackingGetter(bool defaultIsTracking)
            {
                _DefaultIsTracking = defaultIsTracking;
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "AsNoTracking")
                {
                    _hasAsNoTracking = true;

                    return node;
                }
                else if (node.Method.Name == "AsTracking")
                {
                    _hasAsTracking = true;

                    return node;
                }
                else
                {
                    return base.VisitMethodCall(node);
                }
            }

            public bool IsTracking<TElement>(IQueryable<TElement> queryData)
            {
                this.Visit(queryData.Expression);

                if (_hasAsNoTracking)
                {
                    return false;
                }

                if (_hasAsTracking)
                {
                    return true;
                }

                //default value
                return _DefaultIsTracking;
            }
        }

        public class ExpressionIsOrderAppliedGetter : ExpressionVisitor
        {
            private bool _hasOrderByExpression = false;

            public ExpressionIsOrderAppliedGetter()
            {
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "OrderBy" || node.Method.Name == "OrderByDescending")
                {
                    _hasOrderByExpression = true;

                    return node;
                }
                else
                {
                    return base.VisitMethodCall(node);
                }
            }

            public bool IsOrderApplied<TElement>(IQueryable<TElement> queryData)
            {
                this.Visit(queryData.Expression);

                return _hasOrderByExpression;
            }
        }

        public class ExpressionIsTakeAppliedGetter : ExpressionVisitor
        {
            private bool _hasTakeExpression = false;

            public ExpressionIsTakeAppliedGetter()
            {
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "Take")
                {
                    _hasTakeExpression = true;

                    return node;
                }
                else
                {
                    return base.VisitMethodCall(node);
                }
            }

            public bool IsTakeApplied<TElement>(IQueryable<TElement> queryData)
            {
                this.Visit(queryData.Expression);

                return _hasTakeExpression;
            }
        }

        public class ExpressionOrderBeforeTakeOrSkipValidator : ExpressionVisitor
        {
            private int _takeOrSkipExpressionIndex = -1;
            private Type _takeOrSkipEntityType = null;

            private List<int> _orderExpressionIndexes = new List<int>();

            private int index = 0;

            private readonly string[] _takeMethodsName = new string[] { "Take", "TakeLast", "TakeWhile" };
            private readonly string[] _skipMethodsName = new string[] { "Skip", "SkipLast", "SkipWhile" };
            private readonly string[] _orderMethodsName = new string[] { "OrderBy", "OrderByDescending" };

            public ExpressionOrderBeforeTakeOrSkipValidator()
            {
            }

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                index++;

                if (HasOrderBeforeTake())
                {
                    return node;
                }

                if (_takeOrSkipEntityType != null && node.Type.GetGenericArguments()[0] != _takeOrSkipEntityType)
                {
                    return node;
                }

                //just check last Take or skip, if more than one take either query is wrong or too complicated (like there is Select)
                if ((_takeMethodsName.Contains(node.Method.Name) || _skipMethodsName.Contains(node.Method.Name)) && _takeOrSkipExpressionIndex == -1)
                {
                    _takeOrSkipExpressionIndex = index;
                    _takeOrSkipEntityType = node.Type.GetGenericArguments()[0];

                    return base.VisitMethodCall(node);
                }

                if (_takeOrSkipEntityType != null
                    && node.Type.GetGenericArguments()[0] == _takeOrSkipEntityType
                    && _orderMethodsName.Contains(node.Method.Name))
                {
                    _orderExpressionIndexes.Add(index);

                    return base.VisitMethodCall(node);
                }

                return base.VisitMethodCall(node);
            }

            public bool IsMissingOrderBeforeTake<TElement>(IQueryable<TElement> queryData)
            {
                this.Visit(queryData.Expression);

                if (_takeOrSkipExpressionIndex == -1)
                {
                    return false;
                }

                if (_takeOrSkipEntityType == null)
                {
                    //failed to get entity type, maybe not possible but just in case we skip validation
                    return false;
                }

                return !_orderExpressionIndexes.Any(x => x > _takeOrSkipExpressionIndex);
            }

            private bool HasOrderBeforeTake()
            {
                return _takeOrSkipExpressionIndex != -1 && _orderExpressionIndexes.Any(x => x > _takeOrSkipExpressionIndex);
            }
        }
    }
}
