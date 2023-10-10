using ManualIncludableQueryable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ManualIncludableQueryable
{
    public static class DbContextExtensions
    {
        #region one way combine, use entity extension so there will be include chain

        public static TNavigation LoadNavigation<TEntity, TNavigation>(this TEntity entity,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return default(TNavigation);
            }

            var result = new TEntity[] { entity }.LoadNavigation<TEntity, TNavigation>(navigationPropertyPath, dbContext, isTracking: isTracking);

            return result.FirstOrDefault();
        }

        public static List<TNavigation> LoadNavigation<TEntity, TNavigation>(this IEnumerable<TEntity> entities,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isTracking = false,
            bool isOneToOne = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            var manualIncludeType = EFManualIncludableQueryableHelper.GetManualIncludeType(navigationPropertyPath, dbContext);

            switch (manualIncludeType)
            {
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToMany:
                    Type collectionElementType = typeof(TNavigation).GetGenericArguments().First();

                    Type targetType = typeof(IEnumerable<>).MakeGenericType(collectionElementType);

                    LambdaExpression navigationPropertyPathConverted = navigationPropertyPath;

                    if (targetType != typeof(TNavigation))
                    {
                        Type delegateType = typeof(Func<,>).MakeGenericType(typeof(TEntity), targetType);

                        var propertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);

                        var parameter = Expression.Parameter(typeof(TEntity));
                        var memberExpression = Expression.Property(parameter, propertyInfo.Name);

                        navigationPropertyPathConverted = Expression.Lambda(delegateType, memberExpression, parameter);
                    }

                    var result = LoadCollectionManyNavigationsMethodInfo
                        .MakeGenericMethod(typeof(TEntity), collectionElementType)
                        .Invoke(null, new object[] { dbContext, entities, navigationPropertyPathConverted, isTracking });

                    var resultConverted = result as TNavigation;

                    if (resultConverted == null)
                    {
                        return null;
                    }

                    return new List<TNavigation> { resultConverted };

                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToManyUnique:
                    return dbContext.LoadOneToManyUniqueEntities<TEntity, TNavigation>(entities, navigationPropertyPath, isTracking: isTracking);
                case EFManualIncludableQueryableHelper.ManualIncludeType.ManyToOne:
                    return dbContext.LoadManyToOneEntities<TEntity, TNavigation>(entities, navigationPropertyPath, isTracking: isTracking, isOneToOne: isOneToOne);
                default:
                    throw new NotImplementedException();
            }
        }

        public static List<TNavigation> LoadNavigationCollection<TEntity, TNavigation>(this TEntity entity,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            DbContext dbContext,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return new List<TNavigation>();
            }

            return dbContext.LoadManyNavigations(entity, navigationPropertyPath, isTracking: isTracking);
        }

        public static List<TNavigation> LoadNavigationCollection<TEntity, TNavigation>(this IEnumerable<TEntity> entities,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            DbContext dbContext,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            return dbContext.LoadManyNavigations(entities, navigationPropertyPath, isTracking: isTracking);
        }

        #endregion

        #region 2 ways with return value

        private static TNavigation LoadOneNavigation<TEntity, TNavigation>(this DbContext dbContext,
            TEntity entity,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return null;
            }

            var result = dbContext.LoadOneNavigations<TEntity, TNavigation>(new TEntity[] { entity }, navigationPropertyPath, isTracking: isTracking);

            return result?.FirstOrDefault();
        }

        private static List<TNavigation> LoadOneNavigations<TEntity, TNavigation>(this DbContext dbContext,
            IEnumerable<TEntity> entities,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false,
            bool isOneToOne = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            var manualIncludeType = EFManualIncludableQueryableHelper.GetManualIncludeType(navigationPropertyPath, dbContext);

            switch (manualIncludeType)
            {
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToMany:
                    throw new ArgumentException(nameof(TNavigation));
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToManyUnique:
                    return dbContext.LoadOneToManyUniqueEntities<TEntity, TNavigation>(entities, navigationPropertyPath, isTracking: isTracking);
                case EFManualIncludableQueryableHelper.ManualIncludeType.ManyToOne:
                    return dbContext.LoadManyToOneEntities<TEntity, TNavigation>(entities, navigationPropertyPath, isTracking: isTracking, isOneToOne: isOneToOne);
                default:
                    throw new NotImplementedException();
            }
        }

        private static readonly MethodInfo LoadCollectionManyNavigationsMethodInfo = typeof(DbContextExtensions)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(LoadManyNavigations))
            .Single(x => IsIEnumerable(x.GetParameters()[1].ParameterType));

        /// <summary>
        /// Ex. _dbContext.LoadOneToManyEntities(product, x => x.ProductPrices)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entity"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadManyNavigations<TEntity, TNavigation>(this DbContext dbContext,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return new List<TNavigation>();
            }

            return dbContext.LoadOneToManyEntities<TEntity, TNavigation>(entity, navigationPropertyPath, isTracking: isTracking);
        }

        /// <summary>
        /// Ex. _dbContext.LoadOneToManyEntities(products, x => x.ProductPrices)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadManyNavigations<TEntity, TNavigation>(this DbContext dbContext,
            IEnumerable<TEntity> entities,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            return dbContext.LoadOneToManyEntities<TEntity, TNavigation>(entities, navigationPropertyPath, isTracking: isTracking);
        }

        #endregion

        #region original 3 ways

        /// <summary>
        /// Ex. _dbContext.LoadOneToManyEntities(product, x => x.ProductPrices)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entity"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadOneToManyEntities<TEntity, TNavigation>(this DbContext dbContext,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return new List<TNavigation>();
            }

            return dbContext.LoadOneToManyEntities<TEntity, TNavigation>(new TEntity[] { entity }, navigationPropertyPath, isTracking: isTracking);
        }

        /// <summary>
        /// Ex. _dbContext.LoadOneToManyEntities(products, x => x.ProductPrices)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadOneToManyEntities<TEntity, TNavigation>(this DbContext dbContext,
            IEnumerable<TEntity> entities,
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = navigationPropertyPath.GetPropertyInfo();
            var navigation = entityType.FindNavigation(navigationPropertyInfo.Name);

            if (navigation == null)
            {
                throw new ArgumentException("Cannot find navigation property", nameof(navigationPropertyPath));
            }

            var navigationForeignKey = navigation.ForeignKey;

            if (navigation.ForeignKey.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var navigationForeignKeyProperty = navigation.ForeignKey.Properties.Single();

            var inversePkNavigationProperty = navigationForeignKey.DependentToPrincipal;

            var inversePkNavigationPropertyInfo = inversePkNavigationProperty.PropertyInfo;

            var fkName = navigationForeignKeyProperty.Name;

            if (navigationForeignKey.PrincipalKey.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var pkProperty = navigationForeignKey.PrincipalKey.Properties.Single();

            if (pkProperty.DeclaringEntityType != entityType)
            {
                throw new NotImplementedException("method not support for many to many relationship");
            }

            var entityPks = entityType.FindPrimaryKey();

            if (entityPks.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var entityPk = entityPks.Properties.Single();

            var pkSelector = FastInvoke.BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var primaryKeys = entities.Select(pkSelector).ToList();

            var filterPropertyExpression = GetPropertySelector<TNavigation>(fkName);
            var filterExpression = filterPropertyExpression.ConvertToContainsExpr(primaryKeys);

            var query = dbContext.Set<TNavigation>()
                .AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            query = Queryable.Where(query, (dynamic)filterExpression);

            var allNavigationEntities = query.ToList();

            var fkSelector = FastInvoke.BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationEntitiesLookup = allNavigationEntities.ToLookup(fkSelector);

            var navigationPropertySetter = FastInvoke.BuildUntypedSetter<TEntity>(navigationPropertyInfo);
            var inversePkNavigationPropertySetter = FastInvoke.BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo);

            foreach (var entity in entities)
            {
                var keyValueObj = pkSelector(entity);
                var navigationEntities = navigationEntitiesLookup.FirstOrDefault(x => object.Equals(x.Key, keyValueObj));

                if (navigationEntities != null)
                {
                    var navigationEntitiesList = navigationEntities.ToList();

                    navigationPropertySetter(entity, navigationEntitiesList);

                    navigationEntitiesList.ForEach(x => inversePkNavigationPropertySetter(x, entity));
                }
            }

            return allNavigationEntities;
        }

        /// <summary>
        /// Ex. _dbContext.LoadManyToOneEntities(productPrice, x => x.Product)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entity"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static TNavigation LoadManyToOneEntities<TEntity, TNavigation>(this DbContext dbContext,
            TEntity entity,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return null;
            }

            var result = dbContext.LoadManyToOneEntities<TEntity, TNavigation>(new TEntity[] { entity },
                navigationPropertyPath,
                isTracking: isTracking,
                //just one entity so must be one to one
                isOneToOne: true);

            return result?.FirstOrDefault();
        }

        /// <summary>
        /// Ex. _dbContext.LoadManyToOneEntities(productPrices, x => x.Product)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadManyToOneEntities<TEntity, TNavigation>(this DbContext dbContext,
            IEnumerable<TEntity> entities,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false,
            bool isOneToOne = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            if (IsIEnumerable(typeof(TNavigation)))
            {
                throw new ArgumentException(nameof(TNavigation));
            }

            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = navigationPropertyPath.GetPropertyInfo();
            var navigation = entityType.FindNavigation(navigationPropertyInfo.Name);

            if (navigation == null)
            {
                throw new ArgumentException("Cannot find navigation property", nameof(navigationPropertyPath));
            }

            var navigationForeignKey = navigation.ForeignKey;

            //ProductPrice.ProductId
            if (navigation.ForeignKey.Properties.Count != 1)
            {
                throw new NotImplementedException("No FK or more than one FK column");
            }

            var navigationForeignKeyProperty = navigation.ForeignKey.Properties.Single();

            var navigationForeignKeyPropertyInfo = navigationForeignKeyProperty.PropertyInfo;

            var isNullableFk = IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var keyValueSelector = FastInvoke.BuildUntypedGetter<TEntity>(navigationForeignKeyPropertyInfo);
            var keyValuesQuery = entities.Select(keyValueSelector);

            if (isNullableFk)
            {
                keyValuesQuery = keyValuesQuery.Where(x => x != null);
            }

            if (!isOneToOne)
            {
                keyValuesQuery = keyValuesQuery.Distinct();
            }

            var keyValues = keyValuesQuery.ToList();

            ////Product.ProductPrices
            //var inverseCollectionProperty = navigationForeignKey.DependentToPrincipal;

            //var inverseCollectionPropertyInfo = inverseCollectionProperty.PropertyInfo;

            ////ProductId
            //var ProductId = navigationForeignKeyProperty.Name;

            //Product.ProductId
            if (navigationForeignKey.PrincipalKey.Properties.Count != 1)
            {
                throw new NotImplementedException("No PK or more than one PK column");
            }

            var pkProperty = navigationForeignKey.PrincipalKey.Properties.Single();

            if (pkProperty.DeclaringEntityType.Name != typeof(TNavigation).FullName)
            {
                throw new NotImplementedException("method not support for many to many relationship");
            }

            //ProductId
            var pkName = pkProperty.Name;

            var filterPropertyExpression = GetPropertySelector<TNavigation>(pkName);
            var filterExpression = filterPropertyExpression.ConvertToContainsExpr(keyValues);

            var query = dbContext.Set<TNavigation>()
                .AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            query = Queryable.Where(query, (dynamic)filterExpression);

            var navigationEntities = query.ToList();

            var pkValueSelector = GetPropertySelector<TNavigation, int>(pkName).Compile();
            var navigationPropertySetter = FastInvoke.BuildUntypedSetter<TEntity>(navigationPropertyInfo);

            var fkSelector = FastInvoke.BuildUntypedGetter<TEntity>(navigationForeignKeyPropertyInfo);

            foreach (var entity in entities)
            {
                var keyValueObj = fkSelector(entity);

                if (keyValueObj == null)
                {
                    continue;
                }

                var navigationEntity = navigationEntities.FirstOrDefault(x => object.Equals(pkValueSelector(x), keyValueObj));
                navigationPropertySetter(entity, navigationEntity);
            }

            return navigationEntities;
        }

        /// <summary>
        /// A one-to-many relationship with unique index 
        /// Ex. _dbContext.LoadOneToManyEntities(ClientUser, x => x.ClientUserProfile)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static TNavigation LoadOneToManyUniqueEntities<TEntity, TNavigation>(this DbContext dbContext,
            TEntity entity,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entity == null)
            {
                return null;
            }

            var result = dbContext.LoadOneToManyUniqueEntities<TEntity, TNavigation>(new TEntity[] { entity },
                navigationPropertyPath,
                isTracking: isTracking);

            return result?.FirstOrDefault();
        }

        /// <summary>
        /// A one-to-many relationship with unique index 
        /// Ex. _dbContext.LoadOneToManyEntities(ClientUser, x => x.ClientUserProfile)
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <typeparam name="TNavigation"></typeparam>
        /// <param name="dbContext"></param>
        /// <param name="entities"></param>
        /// <param name="navigationPropertyPath"></param>
        /// <param name="isTracking"></param>
        private static List<TNavigation> LoadOneToManyUniqueEntities<TEntity, TNavigation>(this DbContext dbContext,
            IEnumerable<TEntity> entities,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isTracking = false)
            where TEntity : class
            where TNavigation : class
        {
            if (entities == null || !entities.Any())
            {
                return new List<TNavigation>();
            }

            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = navigationPropertyPath.GetPropertyInfo();
            var navigation = entityType.FindNavigation(navigationPropertyInfo.Name);

            if (navigation == null)
            {
                throw new ArgumentException("Cannot find navigation property", nameof(navigationPropertyPath));
            }

            var navigationForeignKey = navigation.ForeignKey;

            if (navigation.ForeignKey.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var navigationForeignKeyProperty = navigation.ForeignKey.Properties.Single();

            var inversePkNavigationProperty = navigationForeignKey.DependentToPrincipal;

            var inversePkNavigationPropertyInfo = inversePkNavigationProperty.PropertyInfo;

            var fkName = navigationForeignKeyProperty.Name;

            if (navigationForeignKey.PrincipalKey.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var pkProperty = navigationForeignKey.PrincipalKey.Properties.Single();

            if (pkProperty.DeclaringEntityType != entityType)
            {
                throw new NotImplementedException("method not support for many to many relationship");
            }

            var entityPks = entityType.FindPrimaryKey();

            if (entityPks.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var entityPk = entityPks.Properties.Single();

            var pkSelector = FastInvoke.BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var primaryKeys = entities.Select(pkSelector).ToList();

            var filterPropertyExpression = GetPropertySelector<TNavigation>(fkName);
            var filterExpression = filterPropertyExpression.ConvertToContainsExpr(primaryKeys);

            var query = dbContext.Set<TNavigation>()
                .AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            query = Queryable.Where(query, (dynamic)filterExpression);

            var allNavigationEntities = query.ToList();

            var fkSelector = FastInvoke.BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationPropertySetter = FastInvoke.BuildUntypedSetter<TEntity>(navigationPropertyInfo);
            var inversePkNavigationPropertySetter = FastInvoke.BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo);

            foreach (var entity in entities)
            {
                var keyValueObj = pkSelector(entity);

                if (keyValueObj == null)
                {
                    continue;
                }

                var navigationEntity = allNavigationEntities.FirstOrDefault(x => object.Equals(fkSelector(x), keyValueObj));

                if (navigationEntity == null)
                {
                    //Just like one to many there must be empty list, so if unique key there must be null
                    continue;
                }

                navigationPropertySetter(entity, navigationEntity);

                inversePkNavigationPropertySetter(navigationEntity, entity);
            }

            return allNavigationEntities;
        }

        #endregion

        public static int GetSingleIntegerKeyValue<T>(this DbContext dbContext, T entity)
        {
            var keyName = dbContext.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name)
                .Single();

            return (int)entity.GetType().GetProperty(keyName).GetValue(entity, null);
        }

        public static Expression<Func<T, int>> GetSingleIntegerKeySelector<T>(this DbContext dbContext)
        {
            var keyName = dbContext.Model.FindEntityType(typeof(T)).FindPrimaryKey().Properties
                .Select(x => x.Name)
                .Single();

            return GetPropertySelector<T, int>(keyName);
        }

        private static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
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

        private static Expression<Func<TEntity, TProperty>> GetPropertySelector<TEntity, TProperty>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda<Func<TEntity, TProperty>>(memberExpression, parameter);

            return lambdaExpression;
        }

        private static LambdaExpression GetPropertySelector<TEntity>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TEntity));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda(memberExpression, parameter);

            return lambdaExpression;
        }

        private static LambdaExpression ConvertToEqualsExpr(this LambdaExpression expression, object targetValue)
        {
            var left = expression;
            ParameterExpression pe = left.Parameters.Single();

            var realTargetType = expression.Body.Type;

            Expression right = Expression.Constant(targetValue, realTargetType);

            return Expression.Lambda(Expression.Equal(left.Body, right), pe);
        }

        private static LambdaExpression ConvertToContainsExpr<TProperty>(this LambdaExpression expression, IEnumerable<TProperty> targetValues)
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

            var methods = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Contains")
                .Where(m => m.GetGenericArguments().Length == 1)
                .Select(m => m.MakeGenericMethod(new[] { elemType }));

            MethodInfo containsMethod = (MethodInfo)
                Type.DefaultBinder.SelectMethod(BindingFlags.Static, methods.ToArray(), new[] { cType, realTargetType }, null);

            var call = Expression.Call(containsMethod, collection, expression.Body);

            return Expression.Lambda(call, pe);
        }

        private static bool IsICollection(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetInterfaces()
                            .Any(x => x.IsGenericType &&
                            x.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        private static bool IsIEnumerable(Type type)
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

        private static bool IsNullableType(Type source)
        {
            return source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        static class FastInvoke
        {
            public static Func<T, object> BuildUntypedGetter<T>(MemberInfo memberInfo)
            {
                var targetType = memberInfo.DeclaringType;
                var exInstance = Expression.Parameter(targetType, "t");

                var exMemberAccess = Expression.MakeMemberAccess(exInstance, memberInfo);
                var exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));
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
        }
    }
}
