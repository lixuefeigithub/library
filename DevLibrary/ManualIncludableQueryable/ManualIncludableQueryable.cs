using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ManualIncludableQueryable
{
    #region node (single chain)

    internal interface IIncludedNavigationQueryChainNode
    {
        IIncludedNavigationQueryChainNode PreviousNode { get; }

        IIncludedNavigationQueryChainNode NextNode { get; }

        void AppendNextNode(IIncludedNavigationQueryChainNode nextNode);

        bool IsOneToOne { get; }

        bool IsReGenerateNavigationQueryByPkOrFk { get; }

        string NavigationPropertyName { get; }

        IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery, bool isUseJoin);

        IQueryable CachedNavigationQueryNoType { get; }

        ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null);

        IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities);

        /// <summary>
        /// null = unknown (like one to many), true = all loaded, false = at least one entity not loaded
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        bool? IsAllNavigationsLoadedNoType(IEnumerable<object> entities);

        Type LastEntityType { get; }
        Type LastNavigationType { get; }

        string FKName { get; }

        string FKNameChain { get; }

        int LastEntityOffsetFromFirstEntity { get; }
    }

    internal interface IIncludedNavigationQueryChainNode<TLastNavigation> : IIncludedNavigationQueryChainNode
        where TLastNavigation : class
    {
        IQueryable<TLastNavigation> CachedNavigationQuery { get; }
    }

    internal interface IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery, bool isUseJoin);

        ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null);

        IEnumerable<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities);

        /// <summary>
        /// null = unknown (like one to many), true = all loaded, false = at least one entity not loaded
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        bool? IsAllNavigationsLoaded(IEnumerable<TLastEntity> entities);
    }

    #endregion

    #region query (multiple chain)

    public interface IManualIncludableQueryable<out TEntity>
        where TEntity : class
    {
        dynamic InvokeQueryToList();

        dynamic InvokeQueryToArray();

        TEntity InvokeQueryFirstOrDefault();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQueryFirstOrDefault(LambdaExpression predicate);

        TEntity InvokeQueryFirst();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQueryFirst(LambdaExpression predicate);

        TEntity InvokeQueryLastOrDefault();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQueryLastOrDefault(LambdaExpression predicate);

        TEntity InvokeQueryLast();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQueryLast(LambdaExpression predicate);

        TEntity InvokeQuerySingleOrDefault();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQuerySingleOrDefault(LambdaExpression predicate);

        TEntity InvokeQuerySingle();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate">Expression<Func<TEntity, bool>> predicate</param>
        /// <returns></returns>
        TEntity InvokeQuerySingle(LambdaExpression predicate);

        IQueryable<TEntity> GetQueryable();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newQueryable">IQueryable<TEntity></param>
        /// <returns></returns>
        IManualIncludableQueryable<TEntity> CreateNewReplaceQueryable(IQueryable newQueryable);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newOrderedQueryable">IOrderedQueryable<TEntity></param>
        /// <returns></returns>
        IOrderedManualIncludableQueryable<TEntity> CreateNewOrderedQueryable(IOrderedQueryable newOrderedQueryable);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="navigationPropertyPath">Expression<Func<TEntity, TNewNavigation>></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isReGenerateNavigationQueryByPkOrFk"></param>
        /// <returns></returns>
        IManualIncludableQueryable<TEntity, TNewNavigation> CreateNewIncludeChainQuery<TNewNavigation>(
            LambdaExpression navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TNewNavigation : class;
    }

    public interface IManualIncludableQueryable<out TEntity, out TLastNavigation> : IManualIncludableQueryable<TEntity>
        where TEntity : class
        where TLastNavigation : class
    {
        IManualIncludableQueryable<TEntity, TNewNavigation> CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(Expression<Func<TPreviousNavigationEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TPreviousNavigationEntity : class
            where TNewNavigation : class;
    }

    public interface IOrderedManualIncludableQueryable<out TEntity> : IManualIncludableQueryable<TEntity>
        where TEntity : class
    {
        IOrderedQueryable<TEntity> GetOrderedQueryable();

        IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable newOrderedQueryable);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNewNavigation"></typeparam>
        /// <param name="navigationPropertyPath">Expression<Func<TEntity, TNewNavigation>></param>
        /// <param name="isOneToOne"></param>
        /// <param name="isReGenerateNavigationQueryByPkOrFk"></param>
        /// <returns></returns>
        new IOrderedManualIncludableQueryable<TEntity, TNewNavigation> CreateNewIncludeChainQuery<TNewNavigation>(
            LambdaExpression navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TNewNavigation : class;
    }

    public interface IOrderedManualIncludableQueryable<out TEntity, out TLastNavigation> : IOrderedManualIncludableQueryable<TEntity>
        where TEntity : class
        where TLastNavigation : class
    {
        IOrderedManualIncludableQueryable<TEntity, TNewNavigation> CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(Expression<Func<TPreviousNavigationEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TPreviousNavigationEntity : class
            where TNewNavigation : class;
    }

    #endregion

    internal static class ManualIncludableQueryableHelper
    {
        #region Invoke

        public static List<TEntity> InvokeQueryToList<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var invokeResult = query.InvokeQueryToList();

            var result = invokeResult as List<TEntity>;

            return result;
        }

        public static TEntity[] InvokeQueryToArray<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var invokeResult = query.InvokeQueryToArray();

            var result = invokeResult as TEntity[];

            return result;
        }

        public static TEntity InvokeQueryFirstOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryFirstOrDefault();

            return result;
        }

        public static TEntity InvokeQueryFirstOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryFirstOrDefault(predicate);

            return result;
        }

        public static TEntity InvokeQueryFirst<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryFirst();

            return result;
        }

        public static TEntity InvokeQueryFirst<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryFirst(predicate);

            return result;
        }

        public static TEntity InvokeQueryLastOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryLastOrDefault();

            return result;
        }

        public static TEntity InvokeQueryLastOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryLastOrDefault(predicate);

            return result;
        }

        public static TEntity InvokeQueryLast<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryLast();

            return result;
        }

        public static TEntity InvokeQueryLast<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQueryLast(predicate);

            return result;
        }

        public static TEntity InvokeQuerySingleOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQuerySingleOrDefault();

            return result;
        }

        public static TEntity InvokeQuerySingleOrDefault<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQuerySingleOrDefault(predicate);

            return result;
        }

        public static TEntity InvokeQuerySingle<TEntity>(IManualIncludableQueryable<TEntity> query)
           where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQuerySingle();

            return result;
        }

        public static TEntity InvokeQuerySingle<TEntity>(IManualIncludableQueryable<TEntity> query,
            Expression<Func<TEntity, bool>> predicate)
            where TEntity : class
        {
            if (query == null)
            {
                return null;
            }

            var result = query.InvokeQuerySingle(predicate);

            return result;
        }


        #endregion

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;

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

        public static PropertyInfo GetPropertyInfo(LambdaExpression propertyLambda)
        {
            MemberExpression member = propertyLambda.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));
            }

            PropertyInfo propInfo = member.Member as PropertyInfo;

            if (propInfo == null)
            {
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));
            }

            return propInfo;
        }

        public static Expression<Func<TSource, TProperty>> GetPropertySelector<TSource, TProperty>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TSource));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda<Func<TSource, TProperty>>(memberExpression, parameter);

            return lambdaExpression;
        }

        public static LambdaExpression GetPropertySelector<TSource>(string properyName, Type realProperyType, Type targetPropertyType = null)
        {
            Type delegateType = typeof(Func<,>).MakeGenericType(typeof(TSource), targetPropertyType ?? realProperyType);

            var parameter = Expression.Parameter(typeof(TSource));
            var memberExpression = Expression.Property(parameter, properyName);

            if (targetPropertyType != null && targetPropertyType != realProperyType)
            {
                var memberExpressionConverted = Expression.Convert(memberExpression, targetPropertyType);

                var lambdaExpressionConverted = Expression.Lambda(delegateType, memberExpressionConverted, parameter);

                return lambdaExpressionConverted;
            }

            var lambdaExpression = Expression.Lambda(delegateType, memberExpression, parameter);

            return lambdaExpression;
        }

        public static LambdaExpression GetPropertySelector<TSource>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TSource));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda(memberExpression, parameter);

            return lambdaExpression;
        }

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

        public static LambdaExpression ConvertToContainsExpr<TProperty>(LambdaExpression expression, IEnumerable<TProperty> targetValues)
        {
            ParameterExpression pe = expression.Parameters.Single();

            if (targetValues == null || !targetValues.Any())
            {
                var left = expression;

                Expression right = Expression.Constant(false, typeof(bool));

                return Expression.Lambda(right, pe);
            }

            Expression collection = Expression.Constant(targetValues);

            var realTargetType = expression.Body.Type;

            if (realTargetType != typeof(TProperty))
            {
                var listType = typeof(List<>).MakeGenericType(realTargetType);
                var list = (IList)Activator.CreateInstance(listType);

                foreach (var obj in targetValues)
                {
                    list.Add(obj);
                }

                collection = Expression.Constant(list);
            }

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

            return Expression.Lambda(call, pe);
        }

        public static LambdaExpression ConvertToEqualsExpr(LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.Equal(left.Body, right), pe);
        }

        public static LambdaExpression ConvertToNotEqualsExpr(LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            Expression right = Expression.Constant(targetValue);

            return Expression.Lambda(Expression.NotEqual(left.Body, right), pe);
        }

        public static LambdaExpression ConvertToNotExpr(LambdaExpression expression)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            return Expression.Lambda(Expression.Not(left.Body), pe);
        }

        public static Expression<Func<T, T, bool>> And<T>(Expression<Func<T, T, bool>> first, Expression<Func<T, T, bool>> second)
        {
            return Compose(first, second, Expression.And);
        }

        public static T Construct<T>(Type[] paramTypes, object[] paramValues)
        {
            Type t = typeof(T);

            ConstructorInfo ci = t.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null, paramTypes, null);

            return (T)ci.Invoke(paramValues);
        }

        private static Expression<T> Compose<T>(Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);

            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static bool IsIEnumerable(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var interfaces = type.GetInterfaces();

            if (interfaces.Any(x => x == typeof(IEnumerable)))
            {
                return true;
            }

            return interfaces
                            .Any(x => x.IsGenericType &&
                            x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        public static bool IsNullableType(Type source)
        {
            return source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static MethodInfo _joinMethod;

        private static MethodInfo JoinMethod
        {
            get
            {
                if (_joinMethod == null)
                {
                    _joinMethod = typeof(Queryable)
                        .GetTypeInfo()
                        .GetDeclaredMethods("Join")
                        .Single(
                            method => method.IsGenericMethodDefinition
                                && method.GetGenericArguments().Length == 4
                                && method.GetParameters().Length == 5);
                }

                return _joinMethod;
            }
        }

        private static readonly MethodInfo LeftJoinMethod = typeof(ManualIncludableQueryableHelper)
           .GetTypeInfo()
           .GetDeclaredMethods(nameof(LeftJoin))
           .Single();

        private static readonly MethodInfo LeftJoinSelectAllMethod = typeof(ManualIncludableQueryableHelper)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(LeftJoinSelectAll))
            .Single();

        public static IQueryable<TNavigation> BuildJoinQueryToSelectOneToManyNavigation<TEntity, TNavigation>(IQueryable<TEntity> entityOneQuery,
            IQueryable<TNavigation> navigationManyQuery,
            LambdaExpression entityOneSelector,
            LambdaExpression navigationManySelector,
            Type selectorKeyType)
        {
            //Always use left join instead of inner join to avoid SQL Server to create a nested query plan

            var intermediateQuery = BuildLeftInnerJoinSelectAllQuery(outerQuery: entityOneQuery,
                innerQuery: navigationManyQuery,
                outerSelector: entityOneSelector,
                innerSelector: navigationManySelector,
                selectorKeyType: selectorKeyType);

            var query = intermediateQuery
                .Where(x => x.RightInner != null)
                .Select(x => x.RightInner);

            return query;

            //If not use left join
            //return BuildInnerJoinQuerySelectInnerEntity(outerQuery: entityOneQuery,
            //    innerQuery: navigationManyQueryFilteredOutNullableFkForEntity,
            //    outerSelector: entityOneSelector,
            //    innerSelector: navigationManySelector,
            //    selectorKeyType: selectorKeyType);
        }

        public static IQueryable<TNavigation> BuildJoinQueryToSelectOneToManyUniqueNavigation<TEntity, TNavigation>(IQueryable<TEntity> entityOneQuery,
            IQueryable<TNavigation> navigationOneQuery,
            LambdaExpression entityOneSelector,
            LambdaExpression navigationOneSelector,
            Type selectorKeyType)
        {
            //For now we use auto include for one to one, so not a problem with join/left join
            //so far we are not sure if the inner join is a problem in a chain, but we thing it's not a problem
            return BuildInnerJoinQuerySelectInnerEntity(outerQuery: entityOneQuery,
                innerQuery: navigationOneQuery,
                outerSelector: entityOneSelector,
                innerSelector: navigationOneSelector,
                selectorKeyType: selectorKeyType);
        }

        public static IQueryable<TNavigation> BuildJoinQueryToSelectManyToOneNavigation<TEntity, TNavigation>(IQueryable<TEntity> entityManyQueryFilteredOutNullFkForNavigation,
            IQueryable<TNavigation> navigationOneQuery,
            LambdaExpression entityManySelector,
            LambdaExpression navigationOneSelector,
            Type selectorKeyType)
        {
            //Normally if single many to one inner join the SQL will not execute a nested query
            //But when in a chain like many to one, one to many, many to one, it may have a nested query plan
            //So be safer for many to one we use left join too
            return BuildLeftInnerJoinQuerySelectInnerEntity(outerQuery: entityManyQueryFilteredOutNullFkForNavigation,
                innerQuery: navigationOneQuery,
                outerSelector: entityManySelector,
                innerSelector: navigationOneSelector,
                selectorKeyType: selectorKeyType);

            //if not use left join
            //return BuildInnerJoinQuerySelectInnerEntity(outerQuery: entityManyQueryFilteredOutNullFkForNavigation,
            //    innerQuery: navigationOneQuery,
            //    outerSelector: entityManySelector,
            //    innerSelector: navigationOneSelector,
            //    selectorKeyType: selectorKeyType);
        }

        private static IQueryable<TNavigation> BuildInnerJoinQuerySelectInnerEntity<TEntity, TNavigation>(IQueryable<TEntity> outerQuery,
            IQueryable<TNavigation> innerQuery,
            LambdaExpression outerSelector,
            LambdaExpression innerSelector,
            Type selectorKeyType)
        {
            Expression<Func<TEntity, TNavigation, TNavigation>> resultSelector = (left, right) => right;

            object result = JoinMethod
                    .MakeGenericMethod(typeof(TEntity), typeof(TNavigation), selectorKeyType, typeof(TNavigation))
                    .Invoke(null, new object[] { outerQuery, innerQuery, outerSelector, innerSelector, resultSelector });

            var resultQuery = result as IQueryable<TNavigation>;

            return resultQuery;
        }

        private static IQueryable<TNavigation> BuildLeftInnerJoinQuerySelectInnerEntity<TEntity, TNavigation>(IQueryable<TEntity> outerQuery,
            IQueryable<TNavigation> innerQuery,
            LambdaExpression outerSelector,
            LambdaExpression innerSelector,
            Type selectorKeyType)
        {
            Expression<Func<TEntity, TNavigation, TNavigation>> resultSelector = (left, right) => right;

            object result = LeftJoinMethod
                    .MakeGenericMethod(typeof(TEntity), typeof(TNavigation), selectorKeyType, typeof(TNavigation))
                    .Invoke(null, new object[] { outerQuery, innerQuery, outerSelector, innerSelector, resultSelector });

            var resultQuery = result as IQueryable<TNavigation>;

            return resultQuery;
        }

        private static IQueryable<LeftJoinResult<TEntity, TNavigation>> BuildLeftInnerJoinSelectAllQuery<TEntity, TNavigation>(IQueryable<TEntity> outerQuery,
            IQueryable<TNavigation> innerQuery,
            LambdaExpression outerSelector,
            LambdaExpression innerSelector,
            Type selectorKeyType)
        {
            object result = LeftJoinSelectAllMethod
                    .MakeGenericMethod(typeof(TEntity), typeof(TNavigation), selectorKeyType)
                    .Invoke(null, new object[] { outerQuery, innerQuery, outerSelector, innerSelector });

            var resultQuery = result as IQueryable<LeftJoinResult<TEntity, TNavigation>>;

            return resultQuery;
        }

        private static IQueryable<TResult> LeftJoin<TLeftQuery, TRightQuery, TKey, TResult>(
            this IQueryable<TLeftQuery> leftQuery,
            IQueryable<TRightQuery> rightQuery,
            Expression<Func<TLeftQuery, TKey>> leftQueryKeySelector,
            Expression<Func<TRightQuery, TKey>> rightQueryKeySelector,
            Expression<Func<TLeftQuery, TRightQuery, TResult>> resultSelector)
        {
            var groupJoinQuery = leftQuery.LeftJoinSelectAll(rightQuery, leftQueryKeySelector, rightQueryKeySelector);

            //Old parameter left
            ParameterExpression paramResultSeletorLeft = resultSelector.Parameters.First();
            ParameterExpression paramResultSeletorRight = resultSelector.Parameters.Skip(1).First();

            //New parameter left.Outer
            ParameterExpression paramNewResultSeletor = Expression.Parameter(typeof(LeftJoinResult<TLeftQuery, TRightQuery>), "x");

            MemberExpression paramNewResultSeletorLeftOuter = Expression.Property(paramNewResultSeletor, nameof(LeftJoinResult<TLeftQuery, TRightQuery>.LeftOuter));
            MemberExpression paramNewResultSeletorRightInner = Expression.Property(paramNewResultSeletor, nameof(LeftJoinResult<TLeftQuery, TRightQuery>.RightInner));

            var resultSelectorBodyReplaceLeftOuterParam = new LeftJoinReplacer(paramResultSeletorLeft, paramNewResultSeletorLeftOuter).Visit(resultSelector.Body);
            var resultSelectorBodyReplaceLeftOuterAndRightInnerParams = new LeftJoinReplacer(paramResultSeletorRight, paramNewResultSeletorRightInner).Visit(resultSelectorBodyReplaceLeftOuterParam);

            var newResultSelector = Expression.Lambda<Func<LeftJoinResult<TLeftQuery, TRightQuery>, TResult>>(resultSelectorBodyReplaceLeftOuterAndRightInnerParams,
                paramNewResultSeletor);

            return groupJoinQuery.Select(newResultSelector);
        }

        private static IQueryable<LeftJoinResult<TLeftQuery, TRightQuery>> LeftJoinSelectAll<TLeftQuery, TRightQuery, TKey>(
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

        public static Func<T, object> BuildUntypedGetter<T>(MemberInfo memberInfo)
        {
            var targetType = memberInfo.DeclaringType;
            var exInstance = Expression.Parameter(targetType, "t");

            var exMemberAccess = Expression.MakeMemberAccess(exInstance, memberInfo);       // t.PropertyName
            var exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));     // Convert(t.PropertyName, typeof(object))
            var lambda = Expression.Lambda<Func<T, object>>(exConvertToObject, exInstance);

            var action = lambda.Compile();
            return action;
        }

        public static Action<T, object> BuildUntypedSetter<T>(MemberInfo memberInfo)
        {
            var targetType = memberInfo.DeclaringType;
            var exInstance = Expression.Parameter(targetType, "t");

            var exMemberAccess = Expression.MakeMemberAccess(exInstance, memberInfo);

            var exValue = Expression.Parameter(typeof(object), "p");
            var exConvertedValue = Expression.Convert(exValue, GetUnderlyingType(memberInfo));
            var exBody = Expression.Assign(exMemberAccess, exConvertedValue);

            var lambda = Expression.Lambda<Action<T, object>>(exBody, exInstance, exValue);
            var action = lambda.Compile();
            return action;
        }

        private static Type GetUnderlyingType(MemberInfo member)
        {
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

        internal class LambdaComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _lambdaComparer;
            private readonly Func<T, int> _lambdaHash;

            public LambdaComparer(Func<T, T, bool> lambdaComparer) :
                this(lambdaComparer, o => 0)
            {
            }

            public LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
            {
                if (lambdaComparer == null)
                    throw new ArgumentNullException("lambdaComparer");
                if (lambdaHash == null)
                    throw new ArgumentNullException("lambdaHash");

                _lambdaComparer = lambdaComparer;
                _lambdaHash = lambdaHash;
            }

            public bool Equals(T x, T y)
            {
                return _lambdaComparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return _lambdaHash(obj);
            }
        }

        internal class LoadedNavigationInfo
        {
            public Type LastEntityType { get; set; }
            public Type LastNavigationType { get; set; }

            public string FKName { get; set; }

            public string FKNameChain { get; set; }

            public string NavigationPropertyName { get; set; }

            public int LastEntityOffsetFromFirstEntity { get; set; }

            public IEnumerable<object> LoadedNavigations { get; set; } = new List<object>();

            public IQueryable CurrentQuery { get; set; }
        }

        internal class IncludedNavigationQueryChainNodeInvokeQueryResult<TNavigation>
            where TNavigation : class
        {
            public List<TNavigation> Navigations { get; set; } = new List<TNavigation>();

            public List<LoadedNavigationInfo> LoadedNavigations { get; set; } = new List<LoadedNavigationInfo>();
        }

        internal class IncludedNavigationQueryChainNodeInvokeQueryResultNoType
        {
            public IEnumerable<object> Navigations { get; set; } = new List<object>();

            public List<LoadedNavigationInfo> LoadedNavigations { get; set; } = new List<LoadedNavigationInfo>();
        }

        private class LeftJoinResult<TLeftOuter, TRightInner>
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