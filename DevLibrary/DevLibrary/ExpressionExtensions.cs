using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqLibrary
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.And);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.Or);
        }

        public static Expression<Func<TEntity, bool>> Equal<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> Equal<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> NotEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.NotEqual(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> NotEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.NotEqual(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> GreaterThan<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThan(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> GreaterThan<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThan(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> GreaterThanOrEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThanOrEqual(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> GreaterThanOrEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThanOrEqual(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> LessThan<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThan(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> LessThan<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThan(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> LessThanOrEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> first, Expression<Func<TEntity, TProperty>> second)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThanOrEqual(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<TEntity, bool>> LessThanOrEqual<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, TProperty targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue, typeof(TProperty));

            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThanOrEqual(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> Not<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(left.Body), pe);
        }

        private static Expression<T> Compose<T>(Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> map;

            public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression p)
            {
                ParameterExpression replacement;

                if (map.TryGetValue(p, out replacement))
                {
                    p = replacement;
                }

                return base.VisitParameter(p);
            }
        }
    }
}
