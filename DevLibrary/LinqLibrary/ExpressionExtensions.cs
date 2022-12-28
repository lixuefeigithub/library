using LinqLibrary.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqLibrary
{
    public static class ExpressionExtensions
    {
        private static List<MethodInfo> _containsMethods;

        private static List<MethodInfo> ContainsMethods
        {
            get
            {
                if (_containsMethods == null)
                {
                    _containsMethods = typeof(Enumerable)
                        .GetTypeInfo()
                        .GetDeclaredMethods("Contains")
                        .Where(m => m.GetGenericArguments().Length == 1)
                        .ToList();
                }

                return _containsMethods;
            }
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.And);
        }

        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.AndAlso);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.Or);
        }

        public static Expression<Func<T, TResult>> Add<T, TResult>(this Expression<Func<T, TResult>> first, Expression<Func<T, TResult>> second)
        {
            return Compose(first, second, Expression.Add);
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

        public static Expression<Func<TEntity, bool>> Equal<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Equal(left.Body, right), pe);
        }

        public static LambdaExpression Equal(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.Equal(left.Body, right), pe);
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

        public static Expression<Func<TEntity, bool>> NotEqual<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.NotEqual(left.Body, right), pe);
        }

        public static LambdaExpression NotEqual(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue);

            return Expression.Lambda(Expression.NotEqual(left.Body, right), pe);
        }

        public static Expression<Func<TEntity, bool>> Contains<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression, IEnumerable<TProperty> targetValues)
        {
            if (targetValues == null || !targetValues.Any())
            {
                return x => false;
            }

            ParameterExpression pe = expression.Parameters.Single();

            Expression collection = Expression.Constant(targetValues);

            Type cType = collection.Type.IsGenericType && collection.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                ? collection.Type
                                : collection.Type.FindInterfaces((m, o) => m.IsGenericType
                                        && m.GetGenericTypeDefinition() == typeof(IEnumerable<>), null)[0];

            collection = Expression.Convert(collection, cType);

            Type elemType = cType.GetGenericArguments()[0];

            var methods = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Contains")
                .Where(m => m.GetGenericArguments().Length == 1)
                .Select(m => m.MakeGenericMethod(new[] { elemType }));

            MethodInfo anyMethod = (MethodInfo)
                Type.DefaultBinder.SelectMethod(BindingFlags.Static, methods.ToArray(), new[] { cType, typeof(TProperty) }, null);

            var call = Expression.Call(anyMethod, collection, expression.Body);

            return Expression.Lambda<Func<TEntity, bool>>(call, pe);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="expression"></param>
        /// <param name="targetValues">should be assignable or convertible for the list type</param>
        /// <returns></returns>
        public static Expression<Func<TEntity, bool>> Contains<TEntity>(this LambdaExpression expression, IEnumerable targetValues)
        {
            ParameterExpression pe = expression.Parameters.Single();

            if (targetValues == null)
            {
                var left = expression;

                Expression right = Expression.Constant(false, typeof(bool));

                return Expression.Lambda<Func<TEntity, bool>>(right, pe);
            }

            var realTargetType = expression.Body.Type;

            var listType = typeof(List<>).MakeGenericType(realTargetType);
            var list = (IList)Activator.CreateInstance(listType);

            foreach (var obj in targetValues)
            {
                if (obj == null)
                {
                    if (!realTargetType.IsValueType || realTargetType.IsNullableType())
                    {
                        list.Add(obj);
                    }

                    continue;
                }

                if (obj.GetType() == realTargetType || realTargetType.IsAssignableFrom(obj.GetType()))
                {
                    list.Add(obj);
                    continue;
                }

                if (realTargetType.GetInterfaces().Any(i => i == typeof(IConvertible)))
                {
                    //If the obj is not convertible for the list table, it will throw error here
                    list.Add(Convert.ChangeType(obj, realTargetType));

                    continue;
                }

                if (realTargetType.IsNullableType() && Nullable.GetUnderlyingType(realTargetType).GetInterfaces().Any(i => i == typeof(IConvertible)))
                {
                    //If the obj is not convertible for the list table, it will throw error here
                    list.Add(Convert.ChangeType(obj, Nullable.GetUnderlyingType(realTargetType)));

                    continue;
                }

                //If cannot convert type then skip
            }

            Expression collection = Expression.Constant(list);

            Type cType = collection.Type.IsGenericType && collection.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                ? collection.Type
                                : collection.Type.FindInterfaces((m, o) => m.IsGenericType
                                        && m.GetGenericTypeDefinition() == typeof(IEnumerable<>), null)[0];

            collection = Expression.Convert(collection, cType);

            Type elemType = cType.GetGenericArguments()[0];

            var methods = ContainsMethods
                .Select(m => m.MakeGenericMethod(new[] { elemType }));

            MethodInfo containsMethod = (MethodInfo)
                Type.DefaultBinder.SelectMethod(BindingFlags.Static, methods.ToArray(), new[] { cType, realTargetType }, null);

            var call = Expression.Call(containsMethod, collection, expression.Body);

            return Expression.Lambda<Func<TEntity, bool>>(call, pe);
        }

        public static Expression<Func<TEntity, bool>> Not<TEntity, TProperty>(this Expression<Func<TEntity, TProperty>> expression)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(left.Body), pe);
        }

        public static Expression<Func<TEntity, bool>> Not<TEntity>(this LambdaExpression expression)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            return Expression.Lambda<Func<TEntity, bool>>(Expression.Not(left.Body), pe);
        }

        public static LambdaExpression Not(this LambdaExpression expression)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            return Expression.Lambda(Expression.Not(left.Body), pe);
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

        public static Expression<Func<TEntity, bool>> GreaterThan<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThan(left.Body, right), pe);
        }

        public static LambdaExpression GreaterThan(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.GreaterThan(left.Body, right), pe);
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

        public static Expression<Func<TEntity, bool>> GreaterThanOrEqual<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.GreaterThanOrEqual(left.Body, right), pe);
        }

        public static LambdaExpression GreaterThanOrEqual(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.GreaterThanOrEqual(left.Body, right), pe);
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

        public static Expression<Func<TEntity, bool>> LessThan<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThan(left.Body, right), pe);
        }

        public static LambdaExpression LessThan(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.LessThan(left.Body, right), pe);
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

        public static Expression<Func<TEntity, bool>> LessThanOrEqual<TEntity>(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda<Func<TEntity, bool>>(Expression.LessThanOrEqual(left.Body, right), pe);
        }

        public static LambdaExpression LessThanOrEqual(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.LessThanOrEqual(left.Body, right), pe);
        }

        public static Expression<Func<TSource, int?>> ConvertToNullableInt<TSource, TEnum>(this Expression<Func<TSource, TEnum>> src)
        {
            return Expression.Lambda<Func<TSource, int?>>(Expression.Convert(src.Body, typeof(int?)), src.Parameters);
        }

        public static Expression<Func<TOuter, TProperty>> ConcactSelector<TInner, TOuter, TProperty>(this Expression<Func<TInner, TProperty>> innerSelector,
            string outerName)
        {
            return ExpressionUtilities.ConcactSelector<TInner, TOuter, TProperty>(innerSelector, outerName);
        }

        public static Expression<Func<TOuter, TProperty>> ConcactSelector<TInner, TOuter, TProperty>(this Expression<Func<TInner, TProperty>> innerSelector,
            Expression<Func<TOuter, TInner>> outerSelector)
        {
            var propertyInfo = outerSelector.GetPropertyInfo();
            return ExpressionUtilities.ConcactSelector<TInner, TOuter, TProperty>(innerSelector, propertyInfo.Name);
        }

        public static Expression<Func<TSource, TResult>> Condition<TSource, TResult>(Expression<Func<TSource, bool>> condition,
            Expression<Func<TSource, TResult>> ifTrue,
            Expression<Func<TSource, TResult>> ifFalse)
        {
            var mapIfTrue = condition.Parameters.Select((f, i) => new { f, s = ifTrue.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            var mapIfFalse = condition.Parameters.Select((f, i) => new { f, s = ifFalse.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            var ifTrueBody = ParameterRebinder.ReplaceParameters(mapIfTrue, ifTrue.Body);
            var ifFalseBody = ParameterRebinder.ReplaceParameters(mapIfFalse, ifFalse.Body);

            return ConditionCore<TSource, TResult>(condition, ifTrueBody, ifFalseBody);
        }

        public static Expression<Func<TSource, TResult>> Condition<TSource, TResult>(Expression<Func<TSource, bool>> condition,
           ConstantExpression ifTrue,
           Expression<Func<TSource, TResult>> ifFalse)
        {
            var mapIfFalse = condition.Parameters.Select((f, i) => new { f, s = ifFalse.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            var ifFalseBody = ParameterRebinder.ReplaceParameters(mapIfFalse, ifFalse.Body);

            return ConditionCore<TSource, TResult>(condition, ifTrue, ifFalseBody);
        }

        public static Expression<Func<TSource, TResult>> Condition<TSource, TResult>(this Expression<Func<TSource, bool>> condition,
           Expression<Func<TSource, TResult>> ifTrue,
           ConstantExpression ifFalse)
        {
            var mapIfTrue = condition.Parameters.Select((f, i) => new { f, s = ifTrue.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            var ifTrueBody = ParameterRebinder.ReplaceParameters(mapIfTrue, ifTrue.Body);

            return ConditionCore<TSource, TResult>(condition, ifTrueBody, ifFalse);
        }

        public static Expression<Func<TSource, TResult>> Condition<TSource, TResult>(this Expression<Func<TSource, bool>> condition,
           ConstantExpression ifTrue,
           ConstantExpression ifFalse)
        {
            return ConditionCore<TSource, TResult>(condition, ifTrue, ifFalse);
        }

        private static Expression<Func<TSource, TResult>> ConditionCore<TSource, TResult>(this Expression<Func<TSource, bool>> condition,
            Expression ifTrueBody,
            Expression ifFalseBody)
        {
            var resultParameter = condition.Parameters;

            var conditionTarget = Expression.Constant(true);

            var resultBody = Expression.Condition(Expression.Equal(condition.Body, conditionTarget), ifTrueBody, ifFalseBody);

            return Expression.Lambda<Func<TSource, TResult>>(resultBody, resultParameter);

            //If Use IfThenElse:
            //var resultBody = Expression.Block(resultBodyAction, Expression.Label(returnTarget, Expression.Constant(default(TResult), typeof(TResult))));
            ////var resultBody = Expression.Block(resultBodyAction, Expression.Label(returnTarget));
            //return Expression.Lambda<Func<TSource, TResult>>(resultBody, resultParameter);
        }

        public static Expression ReplaceParameter<T>(this Expression<T> source, Expression<T> target)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = target.Parameters.Select((f, i) => new { f, s = source.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, source.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return secondBody;
        }

        public static Expression<Func<TSourceNew, TResult>> ReplaceSelectorSourceType<TSourceOld, TSourceNew, TResult>(this Expression<Func<TSourceOld, TResult>> sourceSelector)
        {
            var result = ParameterTypeVisitor<TSourceOld, TSourceNew, TResult>.ToNewType(sourceSelector);

            return result;
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

        private static bool IsNullableType(this Type source)
        {
            return source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            var propInfo = GetMemberInfo(propertyLambda) as PropertyInfo;

            if (propInfo == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));
            }

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));
            }

            return propInfo;
        }

        private static MemberInfo GetMemberInfo(this LambdaExpression memberLambda, bool throwIfNotMember = true)
        {
            MemberExpression member = memberLambda.Body as MemberExpression;

            if (member == null || member.Member == null)
            {
                if (throwIfNotMember)
                {
                    throw new ArgumentException(string.Format(
                        "Expression '{0}' refers to a method, not a property.",
                        memberLambda.ToString()));
                }

                return null;
            }

            return member.Member;
        }

        public static Type GetMemberInfoUnderlyingType(this MemberInfo member)
        {
            if (member == null)
            {
                return null;
            }

            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
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

        class ParameterTypeVisitor<TFrom, TTo, TProperty> : ExpressionVisitor
        {
            private Dictionary<string, MemberInfo> _mappedMemberInfo;

            private Dictionary<string, ParameterExpression> _convertedParameters;

            public ParameterTypeVisitor()
            {
                _mappedMemberInfo = new Dictionary<string, MemberInfo>();
            }

            public ParameterTypeVisitor(ReadOnlyCollection<ParameterExpression> parameterExpressions)
                : this()
            {
                //for each parameter in the original expression creates a new parameter with the same name but with changed type 
                _convertedParameters = parameterExpressions
                    .Where(x => x.Type == typeof(TFrom))
                    .ToDictionary(
                        x => x.Name,
                        x => Expression.Parameter(typeof(TTo), x.Name)
                    );
            }

            public static Expression<Func<TTo, TProperty>> ToNewType(Expression<Func<TFrom, TProperty>> expression)
            {
                var vistior = new ParameterTypeVisitor<TFrom, TTo, TProperty>(expression.Parameters);
                return (Expression<Func<TTo, TProperty>>)vistior.Visit(expression);
            }

            //handles Properties and Fields accessors 
            protected override Expression VisitMember(MemberExpression node)
            {
                //var matchToType = FindToType(node, rootTree)?.Value.newType;
                var matcheToTypeMemberInfo = FindToType(node);

                //we want to replace only the nodes of type TFrom
                //so we can handle expressions of the form x=> x.Property.SubProperty
                //in the expression x=> x.Property1 == 6 && x.Property2 == 3
                //this replaces         ^^^^^^^^^^^         ^^^^^^^^^^^
                //Note: Each level must match
                //Ex. x.Propert1.Sub1.Sub2.Sub3, each level must have a match
                if (matcheToTypeMemberInfo != null)
                {
                    //this will actually call the VisitParameter method in this class
                    var newExp = Visit(node.Expression);

                    var result = Expression.MakeMemberAccess(newExp, matcheToTypeMemberInfo);

                    return result;
                }

                return base.VisitMember(node);
            }

            private MemberInfo FindToType(MemberExpression node)
            {
                var parentNode = node.Expression as MemberExpression;

                var nodeChain = new List<MemberExpression>();

                while (parentNode != null)
                {
                    nodeChain.Insert(0, parentNode);

                    parentNode = parentNode.Expression as MemberExpression;
                }

                nodeChain.Add(node);

                var rootNode = nodeChain[0];

                if (rootNode.Member.DeclaringType != typeof(TFrom))
                {
                    return null;
                }

                if (!_mappedMemberInfo.ContainsKey(rootNode.Member.Name))
                {
                    var targetMemberInfo = GetTargetMemberInfo(rootNode.Member, typeof(TTo));

                    _mappedMemberInfo.Add(rootNode.Member.Name, targetMemberInfo);
                }

                if (nodeChain.Count == 1)
                {
                    return _mappedMemberInfo[rootNode.Member.Name];
                }

                var keyPointer = rootNode.Member.Name;

                var keyForTarget = string.Empty;

                for (int i = 1; i < nodeChain.Count; i++)
                {
                    //GetTargetMemberInfo if return null doesn't make sense to call base.VisitMember
                    //So no null
                    var parentMapping = _mappedMemberInfo[keyPointer];

                    var currentKey = keyPointer + "." + nodeChain[i].Member.Name;

                    var targetToType = parentMapping.GetMemberInfoUnderlyingType();

                    if (!_mappedMemberInfo.ContainsKey(currentKey))
                    {
                        var targetMemberInfo = GetTargetMemberInfo(nodeChain[i].Member, targetToType);

                        _mappedMemberInfo.Add(currentKey, targetMemberInfo);
                    }

                    if (i == nodeChain.Count - 1)
                    {
                        keyForTarget = currentKey;
                    }

                    keyPointer = currentKey;
                }

                if (!string.IsNullOrEmpty(keyForTarget))
                {
                    return _mappedMemberInfo[keyForTarget];
                }

                return null;
            }

            private MemberInfo GetTargetMemberInfo(MemberInfo fromTypeMember, Type toType)
            {
                var mappingAttributes = fromTypeMember
                        .GetCustomAttributes<VisitExpressionReplaceSourceTypeMappingAttribute>();

                var mappingAttribute = mappingAttributes.FirstOrDefault(x => x.IsMatchTargetType(toType));

                var targetMemberName = fromTypeMember.Name;

                if (mappingAttribute != null && !string.IsNullOrWhiteSpace(mappingAttribute.TargetTypePropertyName))
                {
                    targetMemberName = mappingAttribute.TargetTypePropertyName;
                }

                //gets the memberinfo from type TTo that matches the member of type TFrom
                //var memeberInfo = typeof(TTo).GetMember(targetMemberName).FirstOrDefault();
                var memberInfo = toType.GetMember(targetMemberName).FirstOrDefault();

                if (memberInfo == null)
                {
                    throw new Exception("Cannot find target member {targetMemberName}");
                }

                return memberInfo;
            }

            // this will be called where ever we have a reference to a parameter in the expression
            // for ex. in the expression x=> x.Property1 == 6 && x.Property2 == 3
            // this will be called twice     ^                   ^
            protected override Expression VisitParameter(ParameterExpression node)
            {
                var newParameter = _convertedParameters[node.Name];
                return newParameter;
            }

            //this will be the first Visit method to be called
            //since we're converting LambdaExpressions
            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                //visit the body of the lambda, this will Traverse the ExpressionTree 
                //and recursively replace parts of the expression we for which we have matching Visit methods 
                var newExp = Visit(node.Body);

                //this will create the new expression            
                return Expression.Lambda(newExp, _convertedParameters.Select(x => x.Value));
            }
        }
    }
}
