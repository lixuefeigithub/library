using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace EFCore3Library
{
    #region node (single chain)

    internal interface IIncludedNavigationQueryChainNode
    {
        IIncludedNavigationQueryChainNode PreviousNode { get; }

        IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery);

        IQueryable CachedNavigationQueryNoType { get; }

        IEnumerable<object> InvokeQueryNoType(IEnumerable<object> entities, IQueryable sourceQuery, IEnumerable<object> loadedNavigations = null);

        IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities);

        Type LastEntityType { get; }
        Type LastNavigationType { get; }

        string FKName { get; }

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
        IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery);

        List<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            IQueryable<TLastEntity> sourceQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null);

        List<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities);

        IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNextNavigation : class;

        IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyUniqueThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TNextNavigation : class;

        IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateManyToOneThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNextNavigation : class;
    }

    internal class OneToManyIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode PreviousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return PreviousNode;
            }
        }

        private readonly Expression<Func<TLastEntity, IEnumerable<TLastNavigation>>> NavigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            if (sourceQuery.IsMissingOrderBeforeTakeOrSkip())
            {
                //If no order applied the SelectMany Translation is not good, so we do not allow this
                throw new NotImplementedException("Must apply order by before take");
            }

            //For now we didn't find any performance issue with select many so use the select many solution instead of join to be more clean
            //var navigationRawQuery = dbContext.Set<TNavigation>().AsQueryable();

            //if (!sourceQuery.GetIsTracking(dbContext))
            //{
            //    navigationRawQuery = navigationRawQuery.AsNoTracking();
            //}

            //var navigationQuery = sourceQuery.Join(navigationRawQuery, pkSelector, fkSelector, (left, right) => right);

            //var navigationQuery = sourceQuery.SelectMany(navigationPropertySelector);

            var navigationQuery = sourceQuery.SelectMany(NavigationPropertySelector);

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>);
        }

        private readonly PropertyInfo NavigationPropertyInfo;

        private readonly Func<TLastEntity, IEnumerable<TLastNavigation>> NavigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> NavigationPropertyInfoSetter;

        private readonly PropertyInfo NavigationInversePkPropertyInfo;

        private readonly Action<TLastNavigation, object> NavigationInversePkPropertyInfoSetter;

        //type can be int, long or other numeric type, make more generic
        //public Func<TLastEntity, int> PKSelector { get; set; }
        private readonly Func<TLastEntity, object> PKSelector;

        private readonly bool IsFKNullable;

        //type can be int, long, int?, long? or other numeric type, make more generic
        //public Func<TLastNavigation, int> FKSelector { get; set; }
        //public Func<TLastNavigation, int?> FKSelectorNullable { get; set; }

        private readonly Func<TLastNavigation, object> FKSelector;

        private readonly Func<TLastNavigation, object> NavigationPKInverseEntityFKSelector;

        private readonly List<Func<TLastNavigation, object>> NavigationPKSelectors = new List<Func<TLastNavigation, object>>();

        private readonly DbContext DbContext;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string FKName;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return FKName;
            }
        }

        private readonly int LastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return LastEntityOffsetFromFirstEntity;
            }
        }

        public OneToManyIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
            int lastEntityOffsetFromFirstEntity,
            Expression<Func<TLastEntity, IEnumerable<TLastNavigation>>> navigationPropertySelector,
            PropertyInfo navigationPropertyInfo,
            PropertyInfo navigationInversePkPropertyInfo,
            Func<TLastEntity, IEnumerable<TLastNavigation>> navigationPropertyInfoGetter,
            Action<TLastEntity, object> navigationPropertyInfoSetter,
            Action<TLastNavigation, object> navigationInversePkPropertyInfoSetter,
            string fkName,
            Func<TLastEntity, object> pkSelector,
            Func<TLastNavigation, object> fkSelector,
            Func<TLastNavigation, object> navigationPKInverseEntityFKSelector,
            List<Func<TLastNavigation, object>> navigationPKSelectors,
            DbContext dbContext,
            bool isFKNullable)
        {
            PreviousNode = previousNode;
            LastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            NavigationPropertySelector = navigationPropertySelector;
            NavigationPropertyInfo = navigationPropertyInfo;
            NavigationInversePkPropertyInfo = navigationInversePkPropertyInfo;
            NavigationPropertyInfoGetter = navigationPropertyInfoGetter;
            NavigationPropertyInfoSetter = navigationPropertyInfoSetter;
            NavigationInversePkPropertyInfoSetter = navigationInversePkPropertyInfoSetter;
            FKName = fkName;
            PKSelector = pkSelector;
            FKSelector = fkSelector;
            IsFKNullable = isFKNullable;
            NavigationPKInverseEntityFKSelector = navigationPKInverseEntityFKSelector;
            NavigationPKSelectors = navigationPKSelectors;
            DbContext = dbContext;
        }

        public List<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            IQueryable<TLastEntity> sourceQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(sourceQuery);
            var query = navigationQuery;

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            if (hasLoadedNavigations)
            {
                var loadedFKKeysQuery = loadedNavigations.Select(x => FKSelector(x));

                if (IsFKNullable)
                {
                    loadedFKKeysQuery = loadedFKKeysQuery.Where(x => x != null);
                }

                var loadedFKKeys = loadedFKKeysQuery
                    .Distinct()
                    .ToList();

                //if loaded keys to many and we filter by FK Not in (loaded key), it may timed out,
                //so we overwrite the query, filter by FK in (all key - loaded key)

                //Another thing is we don't have conditional include for now, so if one FK loaded, which means all navigations linked to this FK loaded, so we can query by FK instead of PK
                //If we add conditional include in the future, which means we have to change this loaded logic, we have to filter loaded navigations by PK instead of FK, and the it may have more than one PK

                var allFKKeys = entities.Select(PKSelector).ToList();

                var notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                var navigationFkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(FKName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                query = DbContext.Set<TLastNavigation>()
                   .AsQueryable();

                //var isTracking = NavigationQuery.GetIsTracking(DbContext);
                var isTracking = sourceQuery.GetIsTracking(DbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var allNavigationEntities = query.ToList();

            var result = new List<TLastNavigation>();

            var navigationEntitiesLookup = allNavigationEntities
                .Concat(loadedNavigations ?? new List<TLastNavigation>())
                .ToLookup(FKSelector);

            foreach (var entity in entities)
            {
                var keyValueObj = PKSelector(entity);

                var navigationEntities = navigationEntitiesLookup.FirstOrDefault(x => object.Equals(x.Key, keyValueObj));

                if (navigationEntities != null)
                {
                    var navigationEntitiesList = navigationEntities.ToList();

                    result.AddRange(navigationEntitiesList);

                    NavigationPropertyInfoSetter(entity, navigationEntitiesList);

                    navigationEntitiesList.ForEach(x => NavigationInversePkPropertyInfoSetter(x, entity));
                }
            }

            Expression<Func<TLastNavigation, TLastNavigation, bool>> compareExpression = null;

            foreach (var selector in NavigationPKSelectors)
            {
                Expression<Func<TLastNavigation, TLastNavigation, bool>> compareExpressionCurrent = (x, y) => selector(x) == selector(y);

                compareExpression = compareExpression == null
                    ? compareExpressionCurrent
                    : ManualIncludeQueryHelper.And(compareExpression, compareExpressionCurrent);
            }

            var compareFunc = compareExpression.Compile();

            IEqualityComparer<TLastNavigation> comparer = new ManualIncludeQueryHelper.LambdaComparer<TLastNavigation>(compareFunc);

            //For now one to many cannot have duplicated items, unless the upper level entities has the duplicated items
            //Because it cannot have one TNavigation linked to two TEntity at the same time
            //Unless the caller write an inefficient query with duplicated items, there should not be a problem
            //return result.Distinct(comparer).ToList();

            return result;
        }

        public IEnumerable<object> InvokeQueryNoType(IEnumerable<object> entities,
            IQueryable sourceQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            return InvokeQuery(entities.Cast<TLastEntity>(),
                sourceQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());
        }

        public List<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEntities = entities
                .Select(NavigationPropertyInfoGetter)
                .SelectMany(x => x)
                .ToList();

            return allNavigationEntities;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities.Cast<TLastEntity>());
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyUniqueThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateManyToOneThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildManyToOneInclude(
                navigationPropertyPath,
                dbContext,
                this,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return newNode;
        }
    }

    internal class OneToManyUniqueIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode PreviousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return PreviousNode;
            }
        }

        private readonly Expression<Func<TLastEntity, TLastNavigation>> NavigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            if (sourceQuery.IsMissingOrderBeforeTakeOrSkip())
            {
                //If no order applied the SelectMany Translation is not good, so we do not allow this
                throw new NotImplementedException("Must apply order by before take");
            }

            //For now we didn't find any performance issue with select many so use the select many solution instead of join to be more clean
            //var navigationRawQuery = dbContext.Set<TNavigation>().AsQueryable();

            //if (!sourceQuery.GetIsTracking(dbContext))
            //{
            //    navigationRawQuery = navigationRawQuery.AsNoTracking();
            //}

            //var navigationQuery = sourceQuery.Join(navigationRawQuery, pkSelector, fkSelector, (left, right) => right);

            //var navigationQuery = sourceQuery.SelectMany(navigationPropertySelector);

            var navigationQuery = sourceQuery
                .Select(NavigationPropertySelector)
                //treat as one to many there must be empty list, so when unique there must be null
                .Where(x => x != null);

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>);
        }

        private readonly PropertyInfo NavigationPropertyInfo;

        private readonly Func<TLastEntity, TLastNavigation> NavigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> NavigationPropertyInfoSetter;

        private readonly PropertyInfo NavigationInversePkPropertyInfo;

        private readonly Action<TLastNavigation, object> NavigationInversePkPropertyInfoSetter;

        //type can be int, long or other numeric type, make more generic
        //public Func<TLastEntity, int> PKSelector { get; set; }
        private readonly Func<TLastEntity, object> PKSelector;

        private readonly bool IsFKNullable;

        //type can be int, long, int?, long? or other numeric type, make more generic
        //public Func<TLastNavigation, int> FKSelector { get; set; }
        //public Func<TLastNavigation, int?> FKSelectorNullable { get; set; }

        private readonly Func<TLastNavigation, object> FKSelector;

        private readonly Func<TLastNavigation, object> NavigationPKInverseEntityFKSelector;

        private readonly List<Func<TLastNavigation, object>> NavigationPKSelectors = new List<Func<TLastNavigation, object>>();

        private readonly DbContext DbContext;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string FKName;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return FKName;
            }
        }

        private readonly int LastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return LastEntityOffsetFromFirstEntity;
            }
        }

        public OneToManyUniqueIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
            int lastEntityOffsetFromFirstEntity,
            Expression<Func<TLastEntity, TLastNavigation>> navigationPropertySelector,
            PropertyInfo navigationPropertyInfo,
            PropertyInfo navigationInversePkPropertyInfo,
            Func<TLastEntity, TLastNavigation> navigationPropertyInfoGetter,
            Action<TLastEntity, object> navigationPropertyInfoSetter,
            Action<TLastNavigation, object> navigationInversePkPropertyInfoSetter,
            string fkName,
            Func<TLastEntity, object> pkSelector,
            Func<TLastNavigation, object> fkSelector,
            Func<TLastNavigation, object> navigationPKInverseEntityFKSelector,
            List<Func<TLastNavigation, object>> navigationPKSelectors,
            DbContext dbContext,
            bool isFKNullable)
        {
            PreviousNode = previousNode;
            LastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            NavigationPropertySelector = navigationPropertySelector;
            NavigationPropertyInfo = navigationPropertyInfo;
            NavigationInversePkPropertyInfo = navigationInversePkPropertyInfo;
            NavigationPropertyInfoGetter = navigationPropertyInfoGetter;
            NavigationPropertyInfoSetter = navigationPropertyInfoSetter;
            NavigationInversePkPropertyInfoSetter = navigationInversePkPropertyInfoSetter;
            FKName = fkName;
            PKSelector = pkSelector;
            FKSelector = fkSelector;
            IsFKNullable = isFKNullable;
            NavigationPKInverseEntityFKSelector = navigationPKInverseEntityFKSelector;
            NavigationPKSelectors = navigationPKSelectors;
            DbContext = dbContext;
        }

        public List<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            IQueryable<TLastEntity> sourceQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(sourceQuery);
            var query = navigationQuery;

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            if (hasLoadedNavigations)
            {
                var loadedFKKeysQuery = loadedNavigations.Select(x => FKSelector(x));

                if (IsFKNullable)
                {
                    loadedFKKeysQuery = loadedFKKeysQuery.Where(x => x != null);
                }

                var loadedFKKeys = loadedFKKeysQuery
                    .Distinct()
                    .ToList();

                //if loaded keys to many and we filter by FK Not in (loaded key), it may timed out,
                //so we overwrite the query, filter by FK in (all key - loaded key)

                //Another thing is we don't have conditional include for now, so if one FK loaded, which means all navigations linked to this FK loaded, so we can query by FK instead of PK
                //If we add conditional include in the future, which means we have to change this loaded logic, we have to filter loaded navigations by PK instead of FK, and the it may have more than one PK

                var allFKKeys = entities.Select(PKSelector).ToList();

                var notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                var navigationFkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(FKName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                query = DbContext.Set<TLastNavigation>()
                   .AsQueryable();

                var isTracking = sourceQuery.GetIsTracking(DbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var allNavigationEntities = query.ToList();

            var result = new List<TLastNavigation>();

            foreach (var entity in entities)
            {
                var keyValueObj = PKSelector(entity);

                if (keyValueObj == null)
                {
                    continue;
                }

                var navigationEntity = allNavigationEntities.FirstOrDefault(x => object.Equals(FKSelector(x), keyValueObj))
                    ?? loadedNavigations?.FirstOrDefault(x => object.Equals(FKSelector(x), keyValueObj));

                if (navigationEntity == null)
                {
                    //Just like one to many there must be empty list, so if unique key there must be null
                    continue;
                }

                if (!result.Any(x => object.Equals(FKSelector(x), keyValueObj)))
                {
                    result.Add(navigationEntity);
                }

                NavigationPropertyInfoSetter(entity, navigationEntity);

                NavigationInversePkPropertyInfoSetter(navigationEntity, entity);
            }

            return result;
        }

        public IEnumerable<object> InvokeQueryNoType(IEnumerable<object> entities,
            IQueryable sourceQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            return InvokeQuery(entities.Cast<TLastEntity>(),
                sourceQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());
        }

        public List<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEntities = entities
                .Select(NavigationPropertyInfoGetter)
                //.SelectMany(x => x)
                .ToList();

            return allNavigationEntities;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities.Cast<TLastEntity>());
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyUniqueThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateManyToOneThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildManyToOneInclude(
                navigationPropertyPath,
                dbContext,
                this,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return newNode;
        }
    }

    internal class ManyToOneIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode PreviousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return PreviousNode;
            }
        }

        private readonly Expression<Func<TLastEntity, TLastNavigation>> NavigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            var sourceQueryFiltered = sourceQuery;

            if (ManualIncludeQueryHelper.IsNullableType(NavigationForeignKeyPropertyInfo.PropertyType))
            {
                var filterPropertyExpression = ManualIncludeQueryHelper.GetPropertySelector<TLastEntity>(FKName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToNotEqualsExpr(filterPropertyExpression, null);

                sourceQueryFiltered = Queryable.Where(sourceQuery, (dynamic)filterExpression);
            }

            var navigationQuery = sourceQueryFiltered.Select(NavigationPropertySelector);

            if (!IsOneToOne)
            {
                navigationQuery = navigationQuery.Distinct();
            }

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>);
        }

        private readonly PropertyInfo NavigationPropertyInfo;

        private readonly PropertyInfo NavigationForeignKeyPropertyInfo;

        private readonly Func<TLastEntity, TLastNavigation> NavigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> NavigationPropertyInfoSetter;

        private readonly string PKName;

        //PK should be single column in this case, but it can be int, long, or other numeric type, so use untyped selector
        //public Func<TLastNavigation, int> PKSelector { get; set; }
        private readonly Func<TLastNavigation, object> PKSelector;

        private readonly Func<TLastEntity, object> FKSelector;

        private readonly DbContext DbContext;

        private readonly bool IsOneToOne = false;

        private readonly bool IsInvokeDistinctInMemory = false;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string FKName;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return FKName;
            }
        }

        private readonly int LastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return LastEntityOffsetFromFirstEntity;
            }
        }

        public ManyToOneIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
            int lastEntityOffsetFromFirstEntity,
            Expression<Func<TLastEntity, TLastNavigation>> navigationPropertySelector,
            PropertyInfo navigationPropertyInfo,
            PropertyInfo navigationForeignKeyPropertyInfo,
            Func<TLastEntity, TLastNavigation> navigationPropertyInfoGetter,
            Action<TLastEntity, object> navigationPropertyInfoSetter,
            string pkName,
            string fkName,
            Func<TLastNavigation, object> pKSelector,
            Func<TLastEntity, object> fKSelector,
            DbContext dbContext,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
        {

            PreviousNode = previousNode;
            LastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            NavigationPropertySelector = navigationPropertySelector;
            NavigationPropertyInfo = navigationPropertyInfo;
            NavigationForeignKeyPropertyInfo = navigationForeignKeyPropertyInfo;
            NavigationPropertyInfoGetter = navigationPropertyInfoGetter;
            NavigationPropertyInfoSetter = navigationPropertyInfoSetter;
            PKName = pkName;
            FKName = fkName;
            PKSelector = pKSelector;
            FKSelector = fKSelector;
            DbContext = dbContext;
            IsOneToOne = isOneToOne;
            IsInvokeDistinctInMemory = isInvokeDistinctInMemory;
        }

        public List<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            IQueryable<TLastEntity> sourceQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(sourceQuery);
            var query = navigationQuery;

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            //If has too many loaded navigations the query performance is bad, so if has loaded navigation we force re-write query
            var isNeedToOverwriteQuery = hasLoadedNavigations || IsInvokeDistinctInMemory;

            if (isNeedToOverwriteQuery)
            {
                var loadedKeys = new List<object>();

                LambdaExpression filterOutLoadedNavigationsFilter = null;

                var navigationPkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(PKName);

                if (hasLoadedNavigations)
                {
                    loadedKeys = loadedNavigations
                        .Select(x => PKSelector(x))
                        .Distinct()
                        .ToList();

                    var filterPropertyByIds = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationPkSelector, loadedKeys);

                    filterOutLoadedNavigationsFilter = ManualIncludeQueryHelper.ConvertToNotExpr(filterPropertyByIds);
                }

                var keyValues = new List<object>();

                var keyValueQuery = entities.Select(FKSelector).Where(x => x != null);

                if (!IsOneToOne)
                {
                    keyValueQuery = keyValueQuery.Distinct();

                }

                keyValues = keyValueQuery.ToList();

                if (keyValues.Count == 0)
                {
                    //in case all FKs are null no need to load navigations
                    return new List<TLastNavigation>();
                }

                if (hasLoadedNavigations)
                {
                    keyValues = keyValues.Except(loadedKeys).ToList();
                }

                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationPkSelector, keyValues);

                query = DbContext.Set<TLastNavigation>()
                    .AsQueryable();

                //var isTracking = NavigationQuery.GetIsTracking(DbContext);
                var isTracking = sourceQuery.GetIsTracking(DbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var navigationEntities = query.ToList();

            var result = new List<TLastNavigation>();

            foreach (var entity in entities)
            {
                var keyValueObj = FKSelector(entity);

                if (keyValueObj == null)
                {
                    continue;
                }

                var navigationEntity = navigationEntities.FirstOrDefault(x => object.Equals(PKSelector(x), keyValueObj))
                    ?? loadedNavigations?.FirstOrDefault(x => object.Equals(PKSelector(x), keyValueObj));

                if (navigationEntity == null)
                {
                    throw new Exception("Error cannot find entity");
                }

                if (!result.Any(x => object.Equals(PKSelector(x), keyValueObj)))
                {
                    result.Add(navigationEntity);
                }

                NavigationPropertyInfoSetter(entity, navigationEntity);
            }

            return result;
        }

        public IEnumerable<object> InvokeQueryNoType(IEnumerable<object> entities,
            IQueryable sourceQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            return InvokeQuery(entities.Cast<TLastEntity>(),
                sourceQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());
        }

        public List<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEntities = entities
                .Select(NavigationPropertyInfoGetter)
                //for nullable FK
                .Where(x => x != null)
                .ToList();

            return allNavigationEntities;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities.Cast<TLastEntity>());
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateOneToManyUniqueThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude(
                navigationPropertyPath,
                dbContext,
                this);

            return newNode;
        }

        public IIncludedNavigationQueryChainNode<TLastNavigation, TNextNavigation> CreateManyToOneThenIncludeNode<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNextNavigation : class
        {
            var newNode = ManualIncludeQueryHelper.BuildManyToOneInclude(
                navigationPropertyPath,
                dbContext,
                this,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return newNode;
        }
    }

    #endregion

    #region query (multiple chain)

    public interface IManualIncludableQueryable<TEntity>
        //Avoid call Where(), Order(), etc. after call IncludeManual(), since the extension method cannot be override
        //: IQueryable<TEntity>
        where TEntity : class
    {
        List<TEntity> InvokeQueryToList();
        TEntity[] InvokeQueryToArray();
        TEntity InvokeQueryFirstOrDefault();
        TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate);
        TEntity InvokeQueryFirst();
        TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate);
        TEntity InvokeQueryLastOrDefault();
        TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate);
        TEntity InvokeQueryLast();
        TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate);
        TEntity InvokeQuerySingleOrDefault();
        TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate);
        TEntity InvokeQuerySingle();
        TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate);

        IQueryable<TEntity> GetQueryable();

        IManualIncludableQueryable<TEntity> CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable);
        IOrderedManualIncludableQueryable<TEntity> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable);

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
           DbContext dbContext)
            where TNewNavigation : class;

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
           DbContext dbContext)
            where TNewNavigation : class;

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
            where TNewNavigation : class;
    }

    public class ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> : IManualIncludableQueryable<TEntity>
        where TEntity : class
        where TSecondLastNavigation : class
        where TLastNavigation : class
    {
        private readonly IQueryable<TEntity> _queryable;

        public Expression Expression => _queryable.Expression;

        public Type ElementType => _queryable.ElementType;

        public IQueryProvider Provider => _queryable.Provider;

        protected IQueryable<TEntity> Queryable => _queryable;

        public IQueryable<TEntity> GetQueryable()
        {
            return _queryable;
        }

        public ManualIncludableQueryable(IQueryable<TEntity> queryable)
        {
            _queryable = queryable;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return GetEnumerator();
        //}

        internal IIncludedNavigationQueryChainNode<TSecondLastNavigation, TLastNavigation> CurrentNode { get; set; }

        internal List<IIncludedNavigationQueryChainNode> QueryCompletedNodes { get; set; } = new List<IIncludedNavigationQueryChainNode>();

        public ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            if (newQueryable == null)
            {
                throw new ArgumentNullException(nameof(newQueryable));
            }

            var query = new ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation>(newQueryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            return query;
        }

        IManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            return CreateNewReplaceQueryable(newQueryable);
        }

        public OrderedManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            if (newOrderedQueryable == null)
            {
                throw new ArgumentNullException(nameof(newOrderedQueryable));
            }

            var query = new ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation>(newOrderedQueryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            var newOrderedQuery = new OrderedManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation>(query, newOrderedQueryable);

            return newOrderedQuery;
        }

        IOrderedManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            return CreateNewOrderedQueryable(newOrderedQueryable);
        }

        public ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation> CreateOneToManyThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            var node = CurrentNode.CreateOneToManyThenIncludeNode(navigationPropertyPath, dbContext);

            var query = new ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation> CreateOneToManyUniqueThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
           DbContext dbContext)
           where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            var node = CurrentNode.CreateOneToManyUniqueThenIncludeNode(navigationPropertyPath, dbContext);

            var query = new ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation> CreateManyToOneThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            if (ManualIncludeQueryHelper.IsICollection(typeof(TNextNavigation)))
            {
                throw new ArgumentException(nameof(TNextNavigation));
            }

            var node = CurrentNode.CreateManyToOneThenIncludeNode(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            var query = new ManualIncludableQueryable<TEntity, TLastNavigation, TNextNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildOneToManyInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
        {
            var realTargetType = typeof(ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>);

            var query = (ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>)Activator.CreateInstance(realTargetType, this.GetQueryable());

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return query;
        }

        public ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
          DbContext dbContext)
          where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
        {
            var realTargetType = typeof(ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>);

            var query = (ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>)Activator.CreateInstance(realTargetType, this.GetQueryable());

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext);

            return query;
        }

        public ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
           where TNewNavigation : class
        {
            if (ManualIncludeQueryHelper.IsICollection(typeof(TNewNavigation)))
            {
                throw new ArgumentException(nameof(TNewNavigation));
            }

            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(this.Queryable);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
        {
            var realTargetType = typeof(ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>);

            var query = (ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>)Activator.CreateInstance(realTargetType, this.GetQueryable());

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(navigationPropertyPath, dbContext, isOneToOne: isOneToOne, isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateFirstOneToManyIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(queryable);

            var node = ManualIncludeQueryHelper.BuildOneToManyInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateFirstOneToManyUniqueIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(queryable);

            var node = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> CreateFirstManyToOneIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TNewNavigation : class
        {
            if (ManualIncludeQueryHelper.IsICollection(typeof(TNewNavigation)))
            {
                throw new ArgumentException(nameof(TNewNavigation));
            }

            var query = new ManualIncludableQueryable<TEntity, TEntity, TNewNavigation>(queryable);

            var node = ManualIncludeQueryHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TEntity, TEntity> CreateEmptyManualIncludableQueryable(IQueryable<TEntity> queryable)
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity, TEntity>(queryable);

            return query;
        }

        public List<TEntity> InvokeQueryToList()
        {
            var entities = _queryable.ToList();
            IncludeAllNavigations(entities);
            return entities;
        }

        public TEntity[] InvokeQueryToArray()
        {
            var entities = _queryable.ToArray();
            IncludeAllNavigations(entities);
            return entities;
        }

        public TEntity InvokeQueryFirstOrDefault()
        {
            var entity = _queryable.FirstOrDefault();

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.Take(1));
            }

            return entity;
        }

        public TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.FirstOrDefault(predicate);

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).Take(1));
            }

            return entity;
        }

        public TEntity InvokeQueryFirst()
        {
            var entity = _queryable.First();

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.Take(1));

            return entity;
        }

        public TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.First(predicate);

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).Take(1));

            return entity;
        }

        public TEntity InvokeQueryLastOrDefault()
        {
            var entity = _queryable.LastOrDefault();

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.TakeLast(1));
            }

            return entity;
        }

        public TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.LastOrDefault(predicate);

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).TakeLast(1));
            }

            return entity;
        }

        public TEntity InvokeQueryLast()
        {
            var entity = _queryable.Last();

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.TakeLast(1));

            return entity;
        }

        public TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.Last(predicate);

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).TakeLast(1));

            return entity;
        }

        public TEntity InvokeQuerySingleOrDefault()
        {
            var entity = _queryable.SingleOrDefault();

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.Take(1));
            }

            return entity;
        }

        public TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.SingleOrDefault(predicate);

            if (entity != null)
            {
                IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).Take(1));
            }

            return entity;
        }

        public TEntity InvokeQuerySingle()
        {
            var entity = _queryable.Single();

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.Take(1));

            return entity;
        }

        public TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate)
        {
            var entity = _queryable.Single(predicate);

            IncludeAllNavigations(new TEntity[] { entity }, _queryable.Where(predicate).Take(1));

            return entity;
        }

        private void IncludeAllNavigations(IEnumerable<TEntity> entities, IQueryable<TEntity> overwriteQueryable = null)
        {
            var sourceQuery = overwriteQueryable ?? _queryable;

            var allIncludable = QueryCompletedNodes.ToList();

            if (CurrentNode != null && !allIncludable.Contains(CurrentNode))
            {
                allIncludable.Add(CurrentNode);
            }

            var loadedNavigations = new List<LoadedNavigationType>();

            foreach (var includable in allIncludable)
            {
                if (entities == null || !entities.Any())
                {
                    continue;
                }

                var chain = new List<IIncludedNavigationQueryChainNode>();

                chain.Add(includable);

                var pointer = includable;

                while (pointer.PreviousNode != null)
                {
                    chain.Insert(0, pointer.PreviousNode);
                    pointer = pointer.PreviousNode;
                }

                IEnumerable<object> previousEntities = null;
                IQueryable previousQuery = null;

                foreach (var node in chain)
                {
                    var fkNameChain = ManualIncludeQueryHelper.GetIIncludedNavigationQueryChainNodeFKNameChain(node);

                    var sameNavigationLoaded = loadedNavigations
                        .Where(x => x.LastNavigationType == node.LastNavigationType)
                        .Where(x => x.LastEntityType == node.LastEntityType)
                        .Where(x => x.FKNameChain == fkNameChain)
                        .Where(x => x.LastEntityOffsetFromFirstEntity == node.LastEntityOffsetFromFirstEntity)
                        .FirstOrDefault();

                    if (sameNavigationLoaded == null)
                    {
                        var otherLoadedNavigations = loadedNavigations
                            .Where(x => x.LastNavigationType == node.LastNavigationType)
                            .SelectMany(x => x.LoadedNavigations);

                        var invokeResult = node.InvokeQueryNoType(previousEntities ?? entities,
                            previousQuery ?? sourceQuery,
                            otherLoadedNavigations);

                        previousEntities = invokeResult;
                        previousQuery = node.CachedNavigationQueryNoType;

                        loadedNavigations.Add(new LoadedNavigationType
                        {
                            LastEntityType = node.LastEntityType,
                            LastNavigationType = node.LastNavigationType,
                            LastEntityOffsetFromFirstEntity = node.LastEntityOffsetFromFirstEntity,
                            FKName = node.FKName,
                            FKNameChain = fkNameChain,
                            LoadedNavigations = invokeResult,
                            PreviousQuery = previousQuery,
                        });
                    }
                    else
                    {
                        //var loadedEntities = node.GetLoadedNavigationsNoType(previousEntities ?? entities);
                        var loadedEntities = sameNavigationLoaded.LoadedNavigations;
                        previousEntities = loadedEntities;
                        previousQuery = sameNavigationLoaded.PreviousQuery;
                    }
                }
            }
        }

        private class LoadedNavigationType
        {
            public Type LastEntityType { get; set; }
            public Type LastNavigationType { get; set; }

            public string FKName { get; set; }

            public string FKNameChain { get; set; }

            public int LastEntityOffsetFromFirstEntity { get; set; }

            public LambdaExpression PKSelector { get; set; }

            public IEnumerable<object> LoadedNavigations { get; set; } = new List<object>();

            public IQueryable PreviousQuery { get; set; }
        }
    }

    public interface IOrderedManualIncludableQueryable<TEntity> : IManualIncludableQueryable<TEntity>
        where TEntity : class
    {
        IOrderedQueryable<TEntity> GetOrderedQueryable();

        IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable<TEntity> newOrderedQueryable);
    }

    public class OrderedManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> : IOrderedManualIncludableQueryable<TEntity>
        where TEntity : class
        where TSecondLastNavigation : class
        where TLastNavigation : class
    {
        private readonly IOrderedQueryable<TEntity> _orderedQueryable;

        private readonly ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> _manualIncludableQueryable;

        protected ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> ManualIncludableQueryable => _manualIncludableQueryable;

        public IOrderedQueryable<TEntity> GetOrderedQueryable()
        {
            return _orderedQueryable;
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return _orderedQueryable;
        }

        public OrderedManualIncludableQueryable(ManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation> manualIncludeQueryable,
            IOrderedQueryable<TEntity> orderedQueryable)
        {
            this._orderedQueryable = orderedQueryable;
            this._manualIncludableQueryable = manualIncludeQueryable;
        }

        public IManualIncludableQueryable<TEntity> CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            if (newQueryable == null)
            {
                throw new ArgumentNullException(nameof(newQueryable));
            }

            var newManualIncludableQueryable = this.ManualIncludableQueryable.CreateNewReplaceQueryable(newQueryable);

            return newManualIncludableQueryable;
        }

        public IOrderedManualIncludableQueryable<TEntity> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            if (newOrderedQueryable == null)
            {
                throw new ArgumentNullException(nameof(newOrderedQueryable));
            }

            var newManualIncludableQueryable = this.ManualIncludableQueryable.CreateNewReplaceQueryable(newOrderedQueryable);

            var query = new OrderedManualIncludableQueryable<TEntity, TSecondLastNavigation, TLastNavigation>(newManualIncludableQueryable, newOrderedQueryable);

            return query;
        }

        public IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            return CreateNewOrderedQueryable(newOrderedQueryable);
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
        {
            return this.ManualIncludableQueryable.CreateNewOneToManyIncludeChainQuery(navigationPropertyPath,
                dbContext);
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
        {
            return this.ManualIncludableQueryable.CreateNewOneToManyUniqueIncludeChainQuery(navigationPropertyPath,
                dbContext);
        }

        ManualIncludableQueryable<TEntity, TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
             DbContext dbContext,
             bool isOneToOne,
             bool isInvokeDistinctInMemory)
        {
            return this.ManualIncludableQueryable.CreateNewManyToOneIncludeChainQuery(navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);
        }

        public List<TEntity> InvokeQueryToList()
        {
            return _manualIncludableQueryable.InvokeQueryToList();
        }

        public TEntity[] InvokeQueryToArray()
        {
            return _manualIncludableQueryable.InvokeQueryToArray();
        }

        public TEntity InvokeQueryFirstOrDefault()
        {
            return _manualIncludableQueryable.InvokeQueryFirstOrDefault();
        }

        public TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryFirstOrDefault(predicate);
        }

        public TEntity InvokeQueryFirst()
        {
            return _manualIncludableQueryable.InvokeQueryFirst();
        }

        public TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryFirst(predicate);
        }

        public TEntity InvokeQueryLastOrDefault()
        {
            return _manualIncludableQueryable.InvokeQueryLastOrDefault();
        }

        public TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryLastOrDefault(predicate);
        }

        public TEntity InvokeQueryLast()
        {
            return _manualIncludableQueryable.InvokeQueryLast();
        }

        public TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryLast(predicate);
        }

        public TEntity InvokeQuerySingleOrDefault()
        {
            return _manualIncludableQueryable.InvokeQuerySingleOrDefault();
        }

        public TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQuerySingleOrDefault(predicate);
        }

        public TEntity InvokeQuerySingle()
        {
            return _manualIncludableQueryable.InvokeQuerySingle();
        }

        public TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQuerySingle(predicate);
        }
    }

    #endregion

    internal static class ManualIncludeQueryHelper
    {
        public static OneToManyIncludeQueryChainNode<TEntity, TNavigation> BuildOneToManyInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = GetPropertyInfo(navigationPropertyPath);
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

            var navigationForeignKeyPropertyInfo = navigationForeignKeyProperty.PropertyInfo;

            var isFKNullable = IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var fkName = navigationForeignKeyProperty.Name;

            var navigationPropertySelector = GetPropertySelector<TEntity, IEnumerable<TNavigation>>(navigationPropertyInfo.Name);

            var inversePkNavigationProperty = navigationForeignKey.DependentToPrincipal;

            var inversePkNavigationPropertyInfo = inversePkNavigationProperty.PropertyInfo;

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

            var entityPk = entityPks.Properties
                //If include bridge table, like building.ManagerBuildings, the pk > 1, so search pk by name
                .Single();

            var pkSelector = BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var fkSelector = BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationType = dbContext.Model.FindEntityType(typeof(TNavigation).FullName);

            var navigationPks = navigationType.FindPrimaryKey();

            Microsoft.EntityFrameworkCore.Metadata.IProperty navigationPk = null;

            if (navigationPks.Properties.Count == 1)
            {
                navigationPk = navigationPks.Properties.Single();
            }
            else
            {
                //If more than one PK, for now it must be the bridge table, like Building.ManagerBuildings
                //So use FK to search the navigation PK (linked)

                var navigationPksFksMapping = navigationPks.Properties
                    .Select(x => new { PK = x, FKs = x.GetContainingForeignKeys() })
                    .ToList();

                var navigationPkCandidates = navigationPksFksMapping

                    .Where(x => x.FKs.Count() == 1 && x.FKs.Any(f => f.PrincipalEntityType == entityType && f.PrincipalKey.Properties.Count == 1 && f.PrincipalKey.Properties.Single().Name == entityPk.Name))
                    .Select(x => x.PK)
                    .ToList();

                if (navigationPkCandidates.Count != 1)
                {
                    throw new NotImplementedException("method not support for FK > 1");
                }

                navigationPk = navigationPkCandidates.Single();
            }

            var navigationPkSelector = BuildUntypedGetter<TNavigation>(navigationPk.PropertyInfo);

            var navigationPksSelector = navigationPks.Properties
                .Select(x => BuildUntypedGetter<TNavigation>(x.PropertyInfo))
                .ToList();

            var oneToManyIncludeQueryChain = new OneToManyIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationInversePkPropertyInfo: inversePkNavigationPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                navigationInversePkPropertyInfoSetter: BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo),
                fkName: fkName,
                pkSelector: pkSelector,
                fkSelector: fkSelector,
                navigationPKInverseEntityFKSelector: navigationPkSelector,
                navigationPKSelectors: navigationPksSelector,
                dbContext: dbContext,
                isFKNullable: isFKNullable
            );

            return oneToManyIncludeQueryChain;
        }

        public static ManyToOneIncludeQueryChainNode<TEntity, TNavigation> BuildManyToOneInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = GetPropertyInfo(navigationPropertyPath);
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

            var navigationForeignKeyPropertyInfo = navigationForeignKeyProperty.PropertyInfo;

            var fkName = navigationForeignKeyProperty.Name;

            var navigationPropertySelector = GetPropertySelector<TEntity, TNavigation>(navigationPropertyInfo.Name);

            //var inversePkNavigationProperty = navigationForeignKey.DependentToPrincipal;

            //var inversePkNavigationPropertyInfo = inversePkNavigationProperty.PropertyInfo;

            var navigationType = dbContext.Model.FindEntityType(typeof(TNavigation).FullName);

            if (navigationType.FindPrimaryKey().Properties.Count != 1)
            {
                //PK should be single column in this case
                throw new NotImplementedException("No PK found or multiple PK");
            }

            if (navigationForeignKey.PrincipalKey.Properties.Count != 1)
            {
                throw new NotImplementedException("method not support for FK > 1");
            }

            var pkProperty = navigationForeignKey.PrincipalKey.Properties.Single();

            var pkName = pkProperty.Name;

            var pkValueSelector = BuildUntypedGetter<TNavigation>(pkProperty.PropertyInfo);
            var fkFastSelector = BuildUntypedGetter<TEntity>(navigationForeignKeyPropertyInfo);

            var manyToOneIncludeQueryChain = new ManyToOneIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationForeignKeyPropertyInfo: navigationForeignKeyPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                pkName: pkName,
                fkName: fkName,
                pKSelector: pkValueSelector,
                fKSelector: fkFastSelector,
                dbContext: dbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory
            );

            return manyToOneIncludeQueryChain;
        }

        public static OneToManyUniqueIncludeQueryChainNode<TEntity, TNavigation> BuildOneToManyUniqueInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = GetPropertyInfo(navigationPropertyPath);
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

            var navigationForeignKeyPropertyInfo = navigationForeignKeyProperty.PropertyInfo;

            var isFKNullable = IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var fkName = navigationForeignKeyProperty.Name;

            var navigationPropertySelector = GetPropertySelector<TEntity, TNavigation>(navigationPropertyInfo.Name);

            var inversePkNavigationProperty = navigationForeignKey.DependentToPrincipal;

            var inversePkNavigationPropertyInfo = inversePkNavigationProperty.PropertyInfo;

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

            var entityPk = entityPks.Properties
                //If include bridge table, like building.ManagerBuildings, the pk > 1, so search pk by name
                .Single();

            var pkSelector = BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var fkSelector = BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationType = dbContext.Model.FindEntityType(typeof(TNavigation).FullName);

            var navigationPks = navigationType.FindPrimaryKey();

            Microsoft.EntityFrameworkCore.Metadata.IProperty navigationPk = null;

            if (navigationPks.Properties.Count == 1)
            {
                navigationPk = navigationPks.Properties.Single();
            }
            else
            {
                //If more than one PK, for now it must be the bridge table, like Building.ManagerBuildings
                //So use FK to search the navigation PK (linked)

                var navigationPksFksMapping = navigationPks.Properties
                    .Select(x => new { PK = x, FKs = x.GetContainingForeignKeys() })
                    .ToList();

                var navigationPkCandidates = navigationPksFksMapping

                    .Where(x => x.FKs.Count() == 1 && x.FKs.Any(f => f.PrincipalEntityType == entityType && f.PrincipalKey.Properties.Count == 1 && f.PrincipalKey.Properties.Single().Name == entityPk.Name))
                    .Select(x => x.PK)
                    .ToList();

                if (navigationPkCandidates.Count != 1)
                {
                    throw new NotImplementedException("method not support for FK > 1");
                }

                navigationPk = navigationPkCandidates.Single();
            }

            var navigationPkSelector = BuildUntypedGetter<TNavigation>(navigationPk.PropertyInfo);

            var navigationPksSelector = navigationPks.Properties
                .Select(x => BuildUntypedGetter<TNavigation>(x.PropertyInfo))
                .ToList();

            var oneToManyIncludeQueryChain = new OneToManyUniqueIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationInversePkPropertyInfo: inversePkNavigationPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                navigationInversePkPropertyInfoSetter: BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo),
                fkName: fkName,
                pkSelector: pkSelector,
                fkSelector: fkSelector,
                navigationPKInverseEntityFKSelector: navigationPkSelector,
                navigationPKSelectors: navigationPksSelector,
                dbContext: dbContext,
                isFKNullable: isFKNullable
            );

            return oneToManyIncludeQueryChain;
        }

        public static string GetIIncludedNavigationQueryChainNodeFKNameChain(IIncludedNavigationQueryChainNode node)
        {
            if (node == null)
            {
                return null;
            }

            var chain = new List<IIncludedNavigationQueryChainNode>();

            chain.Add(node);

            var pointer = node;

            while (pointer.PreviousNode != null)
            {
                chain.Insert(0, pointer.PreviousNode);
                pointer = pointer.PreviousNode;
            }

            var stringBuilder = new StringBuilder();

            foreach (var item in chain)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(".");
                }

                stringBuilder.Append(item.FKName);
            }

            return stringBuilder.ToString();
        }

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

        public static Expression<Func<TSource, TProperty>> GetPropertySelector<TSource, TProperty>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TSource));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda<Func<TSource, TProperty>>(memberExpression, parameter);

            return lambdaExpression;
        }

        public static LambdaExpression GetPropertySelector<TSource>(string properyName)
        {
            var parameter = Expression.Parameter(typeof(TSource));
            var memberExpression = Expression.Property(parameter, properyName);

            var lambdaExpression = Expression.Lambda(memberExpression, parameter);

            return lambdaExpression;
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

            var methods = typeof(Enumerable).GetMethods()
                .Where(m => m.Name == "Contains")
                .Where(m => m.GetGenericArguments().Length == 1)
                .Select(m => m.MakeGenericMethod(new[] { elemType }));

            MethodInfo containsMethod = (MethodInfo)
                Type.DefaultBinder.SelectMethod(BindingFlags.Static, methods.ToArray(), new[] { cType, realTargetType }, null);

            var call = Expression.Call(containsMethod, collection, expression.Body);

            return Expression.Lambda(call, pe);
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

        private static Expression<T> Compose<T>(Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);


            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static bool IsICollection(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetInterfaces()
                            .Any(x => x.IsGenericType &&
                            x.GetGenericTypeDefinition() == typeof(ICollection<>));
        }

        public static bool IsNullableType(Type source)
        {
            return source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Nullable<>);
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

            // t.PropertValue(Convert(p))
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
    }
}
