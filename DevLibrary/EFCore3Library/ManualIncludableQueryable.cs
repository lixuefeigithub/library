using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        IIncludedNavigationQueryChainNode NextNode { get; }

        void AppendNextNode(IIncludedNavigationQueryChainNode nextNode);

        bool IsOneToOne { get; }

        string NavigationPropertyName { get; }

        IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery, bool isUseJoin);

        IQueryable CachedNavigationQueryNoType { get; }

        ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
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

    internal interface IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery, bool isUseJoin);

        ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
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

    internal class OneToManyIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return _previousNode;
            }
        }

        private IIncludedNavigationQueryChainNode _nextNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.NextNode
        {
            get
            {
                return _nextNode;
            }
        }

        public void AppendNextNode(IIncludedNavigationQueryChainNode nextNode)
        {
            if (nextNode == null)
            {
                throw new ArgumentNullException(nameof(nextNode));
            }

            if (_nextNode != null)
            {
                throw new InvalidOperationException("Next node already set");
            }

            this._nextNode = nextNode;
        }

        public bool IsOneToOne => false;

        private readonly Expression<Func<TLastEntity, IEnumerable<TLastNavigation>>> _navigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery, bool isUseJoin)
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

            IQueryable<TLastNavigation> navigationQuery;

            if (isUseJoin)
            {
                var navigationRawQuery = _dbContext.Set<TLastNavigation>().AsQueryable();

                if (!sourceQuery.GetIsTracking(_dbContext))
                {
                    navigationRawQuery = navigationRawQuery.AsNoTracking();
                }

                navigationQuery = ManualIncludeQueryHelper.BuildJoinQuerySelectInner(sourceQuery,
                    navigationRawQuery,
                    _pkSelectorExpressionForJoin,
                    _fkSelectorExpression,
                    _fkType);
            }
            else
            {
                navigationQuery = sourceQuery.SelectMany(_navigationPropertySelector);
            }

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery, bool isUseJoin)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin);
        }

        private readonly PropertyInfo _navigationPropertyInfo;

        string IIncludedNavigationQueryChainNode.NavigationPropertyName
        {
            get
            {
                return _navigationPropertyInfo.Name;
            }
        }

        private readonly Func<TLastEntity, IEnumerable<TLastNavigation>> _navigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> _navigationPropertyInfoSetter;

        private readonly PropertyInfo _navigationInversePkPropertyInfo;

        private readonly Action<TLastNavigation, object> _navigationInversePkPropertyInfoSetter;

        private readonly Func<TLastEntity, object> _pkSelector;

        private readonly LambdaExpression _pkSelectorExpressionForJoin;

        private readonly Type _pkType;

        private readonly bool _isFKNullable;

        private readonly Func<TLastNavigation, object> _fkSelector;

        private readonly LambdaExpression _fkSelectorExpression;

        private readonly Type _fkType;

        private readonly Func<TLastNavigation, object> _navigationPKInverseEntityFKSelector;

        private readonly List<Func<TLastNavigation, object>> navigationPKSelectors = new List<Func<TLastNavigation, object>>();

        private readonly DbContext _dbContext;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string _fkName;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return _fkName;
            }
        }

        private readonly string _fkNameChain;

        string IIncludedNavigationQueryChainNode.FKNameChain
        {
            get
            {
                return _fkNameChain;
            }
        }

        private readonly int _lastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return _lastEntityOffsetFromFirstEntity;
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
            LambdaExpression pkSelectorExpressionForJoin,
            Type pkType,
            Func<TLastNavigation, object> fkSelector,
            LambdaExpression fkSelectorExpression,
            Type fkType,
            Func<TLastNavigation, object> navigationPKInverseEntityFKSelector,
            List<Func<TLastNavigation, object>> navigationPKSelectors,
            DbContext dbContext,
            bool isFKNullable)
        {
            _previousNode = previousNode;
            _lastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            _navigationPropertySelector = navigationPropertySelector;
            _navigationPropertyInfo = navigationPropertyInfo;
            _navigationInversePkPropertyInfo = navigationInversePkPropertyInfo;
            _navigationPropertyInfoGetter = navigationPropertyInfoGetter;
            _navigationPropertyInfoSetter = navigationPropertyInfoSetter;
            _navigationInversePkPropertyInfoSetter = navigationInversePkPropertyInfoSetter;
            _fkName = fkName;
            _pkSelector = pkSelector;
            _pkSelectorExpressionForJoin = pkSelectorExpressionForJoin;
            _pkType = pkType;
            _fkSelector = fkSelector;
            _fkSelectorExpression = fkSelectorExpression;
            _fkType = fkType;
            _isFKNullable = isFKNullable;
            _navigationPKInverseEntityFKSelector = navigationPKInverseEntityFKSelector;
            this.navigationPKSelectors = navigationPKSelectors;
            _dbContext = dbContext;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = ManualIncludeQueryHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(upperLevelQuery,
                isUseJoin: isCombineOneToOneQueryUsingEFInclude);
            var query = navigationQuery;

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            var loadedNavigationsFilteredForThisInclude = new List<TLastNavigation>();

            var isAllNavigationsAlreadyLoaded = false;

            if (hasLoadedNavigations)
            {
                var loadedFKKeysEnumerable = loadedNavigations.Select(x => _fkSelector(x));

                if (_isFKNullable)
                {
                    loadedFKKeysEnumerable = loadedFKKeysEnumerable.Where(x => x != null);
                }

                var loadedFKKeys = loadedFKKeysEnumerable
                    .Distinct()
                    .ToList();

                //if loaded keys to many and we filter by FK Not in (loaded key), it may timed out,
                //so we overwrite the query, filter by FK in (all key - loaded key)

                //Another thing is we don't have conditional include for now, so if one FK loaded, which means all navigations linked to this FK loaded, so we can query by FK instead of PK
                //If we add conditional include in the future, which means we have to change this loaded logic, we have to filter loaded navigations by PK instead of FK, and the it may have more than one PK

                var allFKKeys = entities.Select(_pkSelector).ToList();

                var notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                isAllNavigationsAlreadyLoaded = notLoadedFKKeys.Count == 0;

                var loadedFKKeysForCurrentInclude = allFKKeys.Except(notLoadedFKKeys).ToList();

                loadedNavigationsFilteredForThisInclude = loadedNavigations
                    .Where(x => loadedFKKeysForCurrentInclude.Contains(_fkSelector(x)))
                    .ToList();

                var navigationFkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(_fkName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                query = _dbContext.Set<TLastNavigation>()
                   .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var invokeQueryCoreResult = ManualIncludeQueryHelper.InvokeQueryCore(this,
                query,
                navigationQuery,
                isCombineOneToOneQueryUsingEFInclude,
                loadedNavigationsFilteredForThisInclude,
                isAllNavigationsAlreadyLoaded: isAllNavigationsAlreadyLoaded);

            result.Navigations = invokeQueryCoreResult.Navigations;
            result.LoadedNavigations = invokeQueryCoreResult.LoadedNavigations;

            var navigationEntitiesLookup = result.Navigations.ToLookup(_fkSelector);

            foreach (var entity in entities)
            {
                var keyValueObj = _pkSelector(entity);

                var navigationEntities = navigationEntitiesLookup.FirstOrDefault(x => object.Equals(x.Key, keyValueObj));

                if (navigationEntities != null)
                {
                    var navigationEntitiesList = navigationEntities.ToList();

                    _navigationPropertyInfoSetter(entity, navigationEntitiesList);

                    navigationEntitiesList.ForEach(x => _navigationInversePkPropertyInfoSetter(x, entity));
                }
            }

            Expression<Func<TLastNavigation, TLastNavigation, bool>> compareExpression = null;

            foreach (var selector in navigationPKSelectors)
            {
                Expression<Func<TLastNavigation, TLastNavigation, bool>> compareExpressionCurrent = (x, y) => selector(x) == selector(y);

                compareExpression = compareExpression == null
                    ? compareExpressionCurrent
                    : ManualIncludeQueryHelper.And(compareExpression, compareExpressionCurrent);
            }

            //Assume one to many will not have duplicated items
            //var compareFunc = compareExpression.Compile();
            //IEqualityComparer<TLastNavigation> comparer = new ManualIncludeQueryHelper.LambdaComparer<TLastNavigation>(compareFunc);
            //return result.Distinct(comparer).ToList();

            return result;
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
            {
                Navigations = typedResult.Navigations,
                LoadedNavigations = typedResult.LoadedNavigations,
            };

            return result;
        }

        public IEnumerable<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEntitiesEnumerable = entities
                .Select(_navigationPropertyInfoGetter)
                .SelectMany(x => x);

            return allNavigationEntitiesEnumerable;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities?.Cast<TLastEntity>());
        }

        public bool? IsAllNavigationsLoaded(IEnumerable<TLastEntity> entities)
        {
            return null;
        }

        public bool? IsAllNavigationsLoadedNoType(IEnumerable<object> entities)
        {
            return null;
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
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return _previousNode;
            }
        }

        private IIncludedNavigationQueryChainNode _nextNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.NextNode
        {
            get
            {
                return _nextNode;
            }
        }

        public void AppendNextNode(IIncludedNavigationQueryChainNode nextNode)
        {
            if (nextNode == null)
            {
                throw new ArgumentNullException(nameof(nextNode));
            }

            if (_nextNode != null)
            {
                throw new InvalidOperationException("Next node already set");
            }

            this._nextNode = nextNode;
        }

        public bool IsOneToOne => true;

        private readonly Expression<Func<TLastEntity, TLastNavigation>> _navigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery, bool isUseJoin)
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

            IQueryable<TLastNavigation> navigationQuery;

            if (isUseJoin)
            {
                var navigationRawQuery = _dbContext.Set<TLastNavigation>().AsQueryable();

                if (!sourceQuery.GetIsTracking(_dbContext))
                {
                    navigationRawQuery = navigationRawQuery.AsNoTracking();
                }

                navigationQuery = ManualIncludeQueryHelper.BuildJoinQuerySelectInner(sourceQuery,
                    navigationRawQuery,
                    _pkSelectorExpression,
                    _fkSelectorExpressionForJoin,
                    _fkType);
            }
            else
            {
                navigationQuery = sourceQuery
                    .Select(_navigationPropertySelector)
                    //treat as one to many there must be empty list, so when unique there must be null
                    .Where(x => x != null);
            }

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery, bool isUseJoin)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin);
        }

        private readonly PropertyInfo _navigationPropertyInfo;

        string IIncludedNavigationQueryChainNode.NavigationPropertyName
        {
            get
            {
                return _navigationPropertyInfo.Name;
            }
        }

        private readonly Func<TLastEntity, TLastNavigation> _navigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> _navigationPropertyInfoSetter;

        private readonly PropertyInfo _navigationInversePkPropertyInfo;

        private readonly Action<TLastNavigation, object> _navigationInversePkPropertyInfoSetter;

        private readonly Func<TLastEntity, object> _pkSelector;

        private readonly LambdaExpression _pkSelectorExpression;

        private readonly Type _pkType;

        private readonly bool _isFKNullable;

        private readonly Func<TLastNavigation, object> _fkSelector;

        private readonly LambdaExpression _fkSelectorExpressionForJoin;

        private readonly Type _fkType;

        private readonly Func<TLastNavigation, object> _navigationPKInverseEntityFKSelector;

        private readonly List<Func<TLastNavigation, object>> _navigationPKSelectors = new List<Func<TLastNavigation, object>>();

        private readonly DbContext _dbContext;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string _fkName;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return _fkName;
            }
        }

        private readonly string _fkNameChain;

        string IIncludedNavigationQueryChainNode.FKNameChain
        {
            get
            {
                return _fkNameChain;
            }
        }

        private readonly int _lastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return _lastEntityOffsetFromFirstEntity;
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
            LambdaExpression pkSelectorExpressionForJoin,
            Type pkType,
            Func<TLastNavigation, object> fkSelector,
            LambdaExpression fkSelectorExpression,
            Type fkType,
            Func<TLastNavigation, object> navigationPKInverseEntityFKSelector,
            List<Func<TLastNavigation, object>> navigationPKSelectors,
            DbContext dbContext,
            bool isFKNullable)
        {
            _previousNode = previousNode;
            _lastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            _navigationPropertySelector = navigationPropertySelector;
            _navigationPropertyInfo = navigationPropertyInfo;
            _navigationInversePkPropertyInfo = navigationInversePkPropertyInfo;
            _navigationPropertyInfoGetter = navigationPropertyInfoGetter;
            this._navigationPropertyInfoSetter = navigationPropertyInfoSetter;
            _navigationInversePkPropertyInfoSetter = navigationInversePkPropertyInfoSetter;
            _fkName = fkName;
            _pkSelector = pkSelector;
            _pkSelectorExpression = pkSelectorExpressionForJoin;
            _pkType = pkType;
            _fkSelector = fkSelector;
            _fkSelectorExpressionForJoin = fkSelectorExpression;
            _fkType = fkType;
            _isFKNullable = isFKNullable;
            _navigationPKInverseEntityFKSelector = navigationPKInverseEntityFKSelector;
            _navigationPKSelectors = navigationPKSelectors;
            _dbContext = dbContext;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = ManualIncludeQueryHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(upperLevelQuery,
                isUseJoin: isCombineOneToOneQueryUsingEFInclude);
            var query = navigationQuery;

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            var loadedNavigationsFilteredForThisInclude = new List<TLastNavigation>();

            var isAllNavigationsAlreadyLoaded = false;

            if (hasLoadedNavigations)
            {
                var loadedFKKeysQuery = loadedNavigations.Select(x => _fkSelector(x));

                if (_isFKNullable)
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

                var allFKKeys = entities.Select(_pkSelector).ToList();

                loadedNavigationsFilteredForThisInclude.AddRange(loadedNavigations.Where(x => allFKKeys.Contains(_fkSelector(x))));

                var notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                isAllNavigationsAlreadyLoaded = notLoadedFKKeys.Count == 0;

                var navigationFkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(_fkName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                query = _dbContext.Set<TLastNavigation>()
                   .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var invokeQueryCoreResult = ManualIncludeQueryHelper.InvokeQueryCore(this,
                query,
                navigationQuery,
                isCombineOneToOneQueryUsingEFInclude,
                loadedNavigationsFilteredForThisInclude,
                isAllNavigationsAlreadyLoaded: isAllNavigationsAlreadyLoaded);

            result.Navigations = invokeQueryCoreResult.Navigations;
            result.LoadedNavigations = invokeQueryCoreResult.LoadedNavigations;

            foreach (var entity in entities)
            {
                var keyValueObj = _pkSelector(entity);

                if (keyValueObj == null)
                {
                    //it's Pk it should not be null
                    throw new NotImplementedException();
                }

                var navigationEntity = result.Navigations.FirstOrDefault(x => object.Equals(_fkSelector(x), keyValueObj));

                if (navigationEntity == null)
                {
                    //Just like one to many there must be empty list, so if unique key there must be null
                    continue;
                }

                _navigationPropertyInfoSetter(entity, navigationEntity);

                _navigationInversePkPropertyInfoSetter(navigationEntity, entity);
            }

            return result;
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
            {
                Navigations = typedResult.Navigations,
                LoadedNavigations = typedResult.LoadedNavigations,
            };

            return result;
        }

        public IEnumerable<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEntitiesEnumerable = entities
                .Select(_navigationPropertyInfoGetter)
                .Where(x => x != null);

            return allNavigationEntitiesEnumerable;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities?.Cast<TLastEntity>());
        }

        public bool? IsAllNavigationsLoaded(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return true;
            }

            if (entities.All(x => _navigationPropertyInfoGetter(x) != null))
            {
                return true;
            }

            return null;
        }

        public bool? IsAllNavigationsLoadedNoType(IEnumerable<object> entities)
        {
            return IsAllNavigationsLoaded(entities?.Cast<TLastEntity>());
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
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.PreviousNode
        {
            get
            {
                return _previousNode;
            }
        }

        private IIncludedNavigationQueryChainNode _nextNode;

        IIncludedNavigationQueryChainNode IIncludedNavigationQueryChainNode.NextNode
        {
            get
            {
                return _nextNode;
            }
        }

        public void AppendNextNode(IIncludedNavigationQueryChainNode nextNode)
        {
            if (nextNode == null)
            {
                throw new ArgumentNullException(nameof(nextNode));
            }

            if (_nextNode != null)
            {
                throw new InvalidOperationException("Next node already set");
            }

            this._nextNode = nextNode;
        }

        private readonly Expression<Func<TLastEntity, TLastNavigation>> _navigationPropertySelector;

        private IQueryable<TLastNavigation> _cachedNavigationQuery { get; set; }

        public IQueryable<TLastNavigation> CachedNavigationQuery => _cachedNavigationQuery;

        public IQueryable CachedNavigationQueryNoType => _cachedNavigationQuery;

        public IQueryable<TLastNavigation> BuildNavigationQuery(IQueryable<TLastEntity> sourceQuery, bool isUseJoin)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            var sourceQueryFiltered = sourceQuery;

            if (ManualIncludeQueryHelper.IsNullableType(_navigationForeignKeyPropertyInfo.PropertyType))
            {
                var filterPropertyExpression = ManualIncludeQueryHelper.GetPropertySelector<TLastEntity>(_fkName);
                var filterExpression = ManualIncludeQueryHelper.ConvertToNotEqualsExpr(filterPropertyExpression, null);

                sourceQueryFiltered = Queryable.Where(sourceQuery, (dynamic)filterExpression);
            }

            IQueryable<TLastNavigation> navigationQuery;

            if (isUseJoin)
            {
                var navigationRawQuery = _dbContext.Set<TLastNavigation>().AsQueryable();

                if (!sourceQuery.GetIsTracking(_dbContext))
                {
                    navigationRawQuery = navigationRawQuery.AsNoTracking();
                }

                navigationQuery = ManualIncludeQueryHelper.BuildJoinQuerySelectInner(sourceQueryFiltered,
                    navigationRawQuery,
                    _fkSelectorExpression,
                    _pkSelectorExpressionForJoin,
                    _fkType);
            }
            else
            {
                navigationQuery = sourceQueryFiltered.Select(_navigationPropertySelector);
            }

            if (!_isOneToOne)
            {
                navigationQuery = navigationQuery.Distinct();
            }

            _cachedNavigationQuery = navigationQuery;

            return navigationQuery;
        }

        public IQueryable BuildNavigationQueryNoType(IQueryable sourceQuery, bool isUseJoin)
        {
            if (sourceQuery == null)
            {
                throw new ArgumentNullException(nameof(sourceQuery));
            }

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin);
        }

        private readonly PropertyInfo _navigationPropertyInfo;

        string IIncludedNavigationQueryChainNode.NavigationPropertyName
        {
            get
            {
                return _navigationPropertyInfo.Name;
            }
        }

        private readonly PropertyInfo _navigationForeignKeyPropertyInfo;

        private readonly Func<TLastEntity, TLastNavigation> _navigationPropertyInfoGetter;

        private readonly Action<TLastEntity, object> _navigationPropertyInfoSetter;

        private readonly string _pkName;

        private readonly Func<TLastNavigation, object> _pkSelector;

        private readonly LambdaExpression _pkSelectorExpressionForJoin;

        private readonly Type _pkType;

        private readonly Func<TLastEntity, object> _fkSelector;

        private readonly LambdaExpression _fkSelectorExpression;

        private readonly Type _fkType;

        private readonly DbContext _dbContext;

        private readonly bool _isOneToOne = false;

        bool IIncludedNavigationQueryChainNode.IsOneToOne
        {
            get
            {
                return _isOneToOne;
            }
        }

        private readonly bool _isInvokeDistinctInMemory = false;

        public Type LastEntityType => typeof(TLastEntity);

        public Type LastNavigationType => typeof(TLastNavigation);

        private readonly string _fkName;

        private readonly bool _isNullableFk;

        string IIncludedNavigationQueryChainNode.FKName
        {
            get
            {
                return _fkName;
            }
        }

        private readonly string _fkNameChain;

        string IIncludedNavigationQueryChainNode.FKNameChain
        {
            get
            {
                return _fkNameChain;
            }
        }

        private readonly int _lastEntityOffsetFromFirstEntity;

        int IIncludedNavigationQueryChainNode.LastEntityOffsetFromFirstEntity
        {
            get
            {
                return _lastEntityOffsetFromFirstEntity;
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
            bool isNullableFk,
            Func<TLastNavigation, object> pKSelector,
            LambdaExpression pkSelectorExpressionForJoin,
            Type pkType,
            Func<TLastEntity, object> fKSelector,
            LambdaExpression fkSelectorExpression,
            Type fkType,
            DbContext dbContext,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
        {
            _previousNode = previousNode;
            _lastEntityOffsetFromFirstEntity = lastEntityOffsetFromFirstEntity;
            _navigationPropertySelector = navigationPropertySelector;
            _navigationPropertyInfo = navigationPropertyInfo;
            _navigationForeignKeyPropertyInfo = navigationForeignKeyPropertyInfo;
            _navigationPropertyInfoGetter = navigationPropertyInfoGetter;
            _navigationPropertyInfoSetter = navigationPropertyInfoSetter;
            _pkName = pkName;
            _fkName = fkName;
            _isNullableFk = isNullableFk;
            _pkSelector = pKSelector;
            _pkSelectorExpressionForJoin = pkSelectorExpressionForJoin;
            _pkType = pkType;
            _fkSelector = fKSelector;
            _fkSelectorExpression = fkSelectorExpression;
            _fkType = fkType;
            _dbContext = dbContext;
            _isOneToOne = isOneToOne;
            _isInvokeDistinctInMemory = isInvokeDistinctInMemory;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = ManualIncludeQueryHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(upperLevelQuery,
                isUseJoin: isCombineOneToOneQueryUsingEFInclude);
            var query = navigationQuery;

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            var loadedNavigationsFilteredForThisInclude = new List<TLastNavigation>();

            //If has too many loaded navigations the query performance is bad, so if has loaded navigation we force re-write query
            var isNeedToOverwriteQuery = hasLoadedNavigations || _isInvokeDistinctInMemory;

            var isAllNavigationsAlreadyLoaded = false;

            if (isNeedToOverwriteQuery)
            {
                var loadedKeys = new List<object>();

                LambdaExpression filterOutLoadedNavigationsFilter = null;

                var navigationPkSelector = ManualIncludeQueryHelper.GetPropertySelector<TLastNavigation>(_pkName);

                if (hasLoadedNavigations)
                {
                    loadedKeys = loadedNavigations
                        .Select(x => _pkSelector(x))
                        .Distinct()
                        .ToList();

                    var filterPropertyByIds = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationPkSelector, loadedKeys);

                    filterOutLoadedNavigationsFilter = ManualIncludeQueryHelper.ConvertToNotExpr(filterPropertyByIds);
                }

                var keyValues = new List<object>();

                var keyValueEnumerable = entities.Select(_fkSelector);

                if (_isNullableFk)
                {
                    keyValueEnumerable = keyValueEnumerable.Where(x => x != null);
                }

                if (!_isOneToOne)
                {
                    keyValueEnumerable = keyValueEnumerable.Distinct();
                }

                keyValues = keyValueEnumerable.ToList();

                if (keyValues.Count == 0)
                {
                    //in case all FKs are null no need to load navigations
                    return result;
                }

                if (hasLoadedNavigations)
                {
                    loadedNavigationsFilteredForThisInclude = loadedNavigations
                        .Where(x => keyValues.Contains(_pkSelector(x)))
                        .ToList();

                    keyValues = keyValues.Except(loadedKeys).ToList();

                    isAllNavigationsAlreadyLoaded = keyValues.Count == 0;
                }

                var filterExpression = ManualIncludeQueryHelper.ConvertToContainsExpr(navigationPkSelector, keyValues);

                query = _dbContext.Set<TLastNavigation>()
                    .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    query = query.AsNoTracking();
                }

                query = Queryable.Where(query, (dynamic)filterExpression);
            }

            var invokeQueryCoreResult = ManualIncludeQueryHelper.InvokeQueryCore(this,
                query,
                navigationQuery,
                isCombineOneToOneQueryUsingEFInclude,
                loadedNavigationsFilteredForThisInclude,
                isAllNavigationsAlreadyLoaded: isAllNavigationsAlreadyLoaded);

            result.Navigations = invokeQueryCoreResult.Navigations;
            result.LoadedNavigations = invokeQueryCoreResult.LoadedNavigations;

            foreach (var entity in entities)
            {
                var keyValueObj = _fkSelector(entity);

                if (keyValueObj == null)
                {
                    continue;
                }

                var navigationEntity = result.Navigations.FirstOrDefault(x => object.Equals(_pkSelector(x), keyValueObj));

                if (navigationEntity == null)
                {
                    throw new Exception("Error cannot find entity");
                }

                _navigationPropertyInfoSetter(entity, navigationEntity);
            }

            return result;
        }

        public ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludeQueryHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
            {
                Navigations = typedResult.Navigations,
                LoadedNavigations = typedResult.LoadedNavigations,
            };

            return result;
        }

        public IEnumerable<TLastNavigation> GetLoadedNavigations(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return new List<TLastNavigation>();
            }

            var allNavigationEnumerable = entities.Select(_navigationPropertyInfoGetter);

            if (_isNullableFk)
            {
                allNavigationEnumerable = allNavigationEnumerable.Where(x => x != null);
            }

            if (!_isOneToOne)
            {
                allNavigationEnumerable = allNavigationEnumerable.Distinct();
            }

            return allNavigationEnumerable;
        }

        public IEnumerable<object> GetLoadedNavigationsNoType(IEnumerable<object> entities)
        {
            return GetLoadedNavigations(entities?.Cast<TLastEntity>());
        }

        public bool? IsAllNavigationsLoaded(IEnumerable<TLastEntity> entities)
        {
            if (entities == null || !entities.Any())
            {
                return true;
            }

            if (entities.Any(x => _fkSelector(x) != null && _navigationPropertyInfoGetter(x) == null))
            {
                return false;
            }

            return true;
        }

        public bool? IsAllNavigationsLoadedNoType(IEnumerable<object> entities)
        {
            return IsAllNavigationsLoaded(entities?.Cast<TLastEntity>());
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

        ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath)
            where TNewNavigation : class;

        ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath)
            where TNewNavigation : class;

        ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
            where TNewNavigation : class;
    }

    public class ManualIncludableQueryable<TEntity, TLastNavigation> : IManualIncludableQueryable<TEntity>
        where TEntity : class
        where TLastNavigation : class
    {
        //For now hard code to false, assume in most cases one query is better than multiple queries per layer (if one to one)
        public readonly bool IsCombineOneToOneQueryUsingEFInclude = true;

        private readonly IQueryable<TEntity> _queryable;

        public Expression Expression => _queryable.Expression;

        public Type ElementType => _queryable.ElementType;

        public IQueryProvider Provider => _queryable.Provider;

        protected IQueryable<TEntity> Queryable => _queryable;

        private readonly ReadOnlyCollection<ManualIncludeQueryHelper.KeySelector<TEntity>> _entityPksSelectorExpression;

        private readonly DbContext _dbContext;

        protected DbContext DbContext => _dbContext;

        public IQueryable<TEntity> GetQueryable()
        {
            return _queryable;
        }

        public ManualIncludableQueryable(IQueryable<TEntity> queryable, DbContext dbContext)
        {
            _queryable = queryable;
            _dbContext = dbContext;

            _entityPksSelectorExpression = ManualIncludeQueryHelper.GetEntityPksSelectorExpression<TEntity>(dbContext);
        }

        private ManualIncludableQueryable(IQueryable<TEntity> queryable,
            DbContext dbContext,
            ReadOnlyCollection<ManualIncludeQueryHelper.KeySelector<TEntity>> entityPksSelectorExpression)
        {
            _queryable = queryable;
            _dbContext = dbContext;
            _entityPksSelectorExpression = entityPksSelectorExpression;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        internal IIncludedNavigationQueryChainNode<TLastNavigation> CurrentNode { get; set; }

        internal List<IIncludedNavigationQueryChainNode> QueryCompletedNodes { get; set; } = new List<IIncludedNavigationQueryChainNode>();

        public ManualIncludableQueryable<TEntity, TLastNavigation> CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            if (newQueryable == null)
            {
                throw new ArgumentNullException(nameof(newQueryable));
            }

            var query = new ManualIncludableQueryable<TEntity, TLastNavigation>(newQueryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            return query;
        }

        IManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            return CreateNewReplaceQueryable(newQueryable);
        }

        public OrderedManualIncludableQueryable<TEntity, TLastNavigation> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            if (newOrderedQueryable == null)
            {
                throw new ArgumentNullException(nameof(newOrderedQueryable));
            }

            var query = new ManualIncludableQueryable<TEntity, TLastNavigation>(newOrderedQueryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            var newOrderedQuery = new OrderedManualIncludableQueryable<TEntity, TLastNavigation>(query, newOrderedQueryable);

            return newOrderedQuery;
        }

        IOrderedManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            return CreateNewOrderedQueryable(newOrderedQueryable);
        }

        public ManualIncludableQueryable<TEntity, TNextNavigation> CreateOneToManyThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, IEnumerable<TNextNavigation>>> navigationPropertyPath)
            where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            var node = CurrentNode.CreateOneToManyThenIncludeNode(navigationPropertyPath, this.DbContext);

            var query = new ManualIncludableQueryable<TEntity, TNextNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TNextNavigation> CreateOneToManyUniqueThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath)
           where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            var node = CurrentNode.CreateOneToManyUniqueThenIncludeNode(navigationPropertyPath, this.DbContext);

            var query = new ManualIncludableQueryable<TEntity, TNextNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TNextNavigation> CreateManyToOneThenIncludeQuery<TNextNavigation>(Expression<Func<TLastNavigation, TNextNavigation>> navigationPropertyPath,
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
                this.DbContext,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            var query = new ManualIncludableQueryable<TEntity, TNextNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            query.CurrentNode = node;

            return query;
        }

        public ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildOneToManyInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                this.DbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath)
        {
            Type[] paramTypes = new Type[] { typeof(IQueryable<TEntity>), typeof(DbContext), typeof(ReadOnlyCollection<ManualIncludeQueryHelper.KeySelector<TEntity>>) };
            object[] paramValues = new object[] { this.GetQueryable(), this.DbContext, this._entityPksSelectorExpression };

            var query = ManualIncludeQueryHelper.Construct<ManualIncludableQueryable<TEntity, TNewNavigation>>(paramTypes, paramValues);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(navigationPropertyPath);

            return query;
        }

        public ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath)
          where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                this.DbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath)
        {
            Type[] paramTypes = new Type[] { typeof(IQueryable<TEntity>), typeof(DbContext), typeof(ReadOnlyCollection<ManualIncludeQueryHelper.KeySelector<TEntity>>) };
            object[] paramValues = new object[] { this.GetQueryable(), this.DbContext, this._entityPksSelectorExpression };

            var query = ManualIncludeQueryHelper.Construct<ManualIncludableQueryable<TEntity, TNewNavigation>>(paramTypes, paramValues);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(navigationPropertyPath);

            return query;
        }

        public ManualIncludableQueryable<TEntity, TNewNavigation> CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isInvokeDistinctInMemory = false)
           where TNewNavigation : class
        {
            if (ManualIncludeQueryHelper.IsICollection(typeof(TNewNavigation)))
            {
                throw new ArgumentException(nameof(TNewNavigation));
            }

            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = ManualIncludeQueryHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                this.DbContext,
                null,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            query.CurrentNode = node;

            return query;
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
        {
            Type[] paramTypes = new Type[] { typeof(IQueryable<TEntity>), typeof(DbContext), typeof(ReadOnlyCollection<ManualIncludeQueryHelper.KeySelector<TEntity>>) };
            object[] paramValues = new object[] { this.GetQueryable(), this.DbContext, this._entityPksSelectorExpression };

            var query = ManualIncludeQueryHelper.Construct<ManualIncludableQueryable<TEntity, TNewNavigation>>(paramTypes, paramValues);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            query = query.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(navigationPropertyPath, isOneToOne: isOneToOne, isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TNewNavigation> CreateFirstOneToManyIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath,
            DbContext dbContext)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(queryable, dbContext);

            var node = ManualIncludeQueryHelper.BuildOneToManyInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TNewNavigation> CreateFirstOneToManyUniqueIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TNewNavigation : class
        {
            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(queryable, dbContext);

            var node = ManualIncludeQueryHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TNewNavigation> CreateFirstManyToOneIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
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

            var query = new ManualIncludableQueryable<TEntity, TNewNavigation>(queryable, dbContext);

            var node = ManualIncludeQueryHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null,
                isOneToOne: isOneToOne,
                isInvokeDistinctInMemory: isInvokeDistinctInMemory);

            query.CurrentNode = node;

            return query;
        }

        public static ManualIncludableQueryable<TEntity, TEntity> CreateEmptyManualIncludableQueryable(IQueryable<TEntity> queryable, DbContext dbContext)
        {
            var query = new ManualIncludableQueryable<TEntity, TEntity>(queryable, dbContext);

            return query;
        }

        public List<TEntity> InvokeQueryToList()
        {
            var queryableWithOneToOneIncludes = BuildEntityQueryWithAllOneToOneIncludes(_queryable);

            var entities = queryableWithOneToOneIncludes.EntityQueryaleWithOneToOneIncludes.ToList();

            var loadedEntityQueryOneToOneNavigationInfos = GetEntityQueryLoadedneToOneNavigationInfos(entities,
                queryableWithOneToOneIncludes.AllOneToOneAutoIncludes);

            IncludeAllNavigations(entities, null, loadedEntityQueryOneToOneNavigationInfos);
            return entities;
        }

        public TEntity[] InvokeQueryToArray()
        {
            var queryableWithOneToOneIncludes = BuildEntityQueryWithAllOneToOneIncludes(_queryable);

            var entities = queryableWithOneToOneIncludes.EntityQueryaleWithOneToOneIncludes.ToArray();

            var loadedEntityQueryOneToOneNavigationInfos = GetEntityQueryLoadedneToOneNavigationInfos(entities,
                queryableWithOneToOneIncludes.AllOneToOneAutoIncludes);

            IncludeAllNavigations(entities, null, loadedEntityQueryOneToOneNavigationInfos);

            return entities;
        }

        public TEntity InvokeQueryFirstOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.FirstOrDefault());
        }

        public TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.FirstOrDefault(predicate));
        }

        public TEntity InvokeQueryFirst()
        {
            return InvokeQueryTakeOneCore(x => x.First());
        }

        public TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.First(predicate));
        }

        public TEntity InvokeQueryLastOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.LastOrDefault());
        }

        public TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.LastOrDefault(predicate));
        }

        public TEntity InvokeQueryLast()
        {
            return InvokeQueryTakeOneCore(x => x.Last());
        }

        public TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.Last(predicate));
        }

        public TEntity InvokeQuerySingleOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.SingleOrDefault());
        }

        public TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.SingleOrDefault(predicate));
        }

        public TEntity InvokeQuerySingle()
        {
            return InvokeQueryTakeOneCore(x => x.Single());
        }

        public TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.Single(predicate));
        }

        private TEntity InvokeQueryTakeOneCore(Func<IQueryable<TEntity>, TEntity> invokeQueryFunc)
        {
            var queryableWithOneToOneIncludes = BuildEntityQueryWithAllOneToOneIncludes(_queryable);

            var entity = invokeQueryFunc(queryableWithOneToOneIncludes.EntityQueryaleWithOneToOneIncludes);

            if (entity != null)
            {
                var overwriteQuery = BuildOverwriteTakeOneQuery(entity);

                var loadedEntityQueryOneToOneNavigationInfos = GetEntityQueryLoadedneToOneNavigationInfos(new TEntity[] { entity },
                    queryableWithOneToOneIncludes.AllOneToOneAutoIncludes);

                IncludeAllNavigations(new TEntity[] { entity }, overwriteQuery, loadedEntityQueryOneToOneNavigationInfos);
            }

            return entity;
        }

        private IQueryable<TEntity> BuildOverwriteTakeOneQuery(TEntity entity)
        {
            if (_entityPksSelectorExpression == null || _entityPksSelectorExpression.Count == 0)
            {
                return _queryable;
            }

            var isTracking = _queryable.GetIsTracking(_dbContext);

            var query = _dbContext.Set<TEntity>()
               .AsQueryable();

            if (!isTracking)
            {
                query = query.AsNoTracking();
            }

            foreach (var pkSelector in _entityPksSelectorExpression)
            {
                var keyValue = pkSelector.UntypedGetter(entity);

                var filterExpression = ManualIncludeQueryHelper.ConvertToEqualsExpr(pkSelector.LambdaExpression, keyValue);

                query = System.Linq.Queryable.Where(query, (dynamic)filterExpression);
            }

            return query;
        }

        private List<List<IIncludedNavigationQueryChainNode>> _cachedAllIncludableOrderedChains = null;

        private List<List<IIncludedNavigationQueryChainNode>> BuildAllIncludableOrderedChains()
        {
            var allIncludable = QueryCompletedNodes.ToList();

            if (CurrentNode != null && !allIncludable.Contains(CurrentNode))
            {
                allIncludable.Add(CurrentNode);
            }

            var allIncludableChains = allIncludable
                .Select(x => ManualIncludeQueryHelper.GetOrderedIIncludedNavigationQueryChainFromLastNode(x))
                .ToList();

            _cachedAllIncludableOrderedChains = allIncludableChains;

            return allIncludableChains;
        }

        private ManualIncludeQueryHelper.BuildQueryWithAllOneToOneIncludesResult<TEntity> BuildEntityQueryWithAllOneToOneIncludes(IQueryable<TEntity> source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new ManualIncludeQueryHelper.BuildQueryWithAllOneToOneIncludesResult<TEntity>
            {
                EntityQueryaleWithOneToOneIncludes = source,
            };

            if (!IsCombineOneToOneQueryUsingEFInclude)
            {
                return result;
            }

            var allIncludableChains = _cachedAllIncludableOrderedChains ?? BuildAllIncludableOrderedChains();

            if (allIncludableChains == null || !allIncludableChains.Any())
            {
                return result;
            }

            var query = source;

            foreach (var includable in allIncludableChains)
            {
                var oneToOneNodesChain = new List<IIncludedNavigationQueryChainNode>();

                var pointer = includable[0];

                while (pointer != null && pointer.IsOneToOne)
                {
                    oneToOneNodesChain.Add(pointer);

                    pointer = pointer.NextNode;
                }

                if (oneToOneNodesChain.Count > 0)
                {
                    result.AllOneToOneAutoIncludes.Add(oneToOneNodesChain);

                    var navigationPath = ManualIncludeQueryHelper.GetIncludeChainNavigationPath(oneToOneNodesChain);

                    query = query.Include(navigationPath);
                }
            }

            result.EntityQueryaleWithOneToOneIncludes = query;

            return result;
        }

        private List<ManualIncludeQueryHelper.LoadedNavigationInfo> GetEntityQueryLoadedneToOneNavigationInfos(IEnumerable<TEntity> entities,
            List<List<IIncludedNavigationQueryChainNode>> allOneToOneAutoIncludes,
            IQueryable<TEntity> overwriteQueryable = null)
        {
            var result = new List<ManualIncludeQueryHelper.LoadedNavigationInfo>();

            if (entities == null || !entities.Any())
            {
                return result;
            }

            foreach (var chain in allOneToOneAutoIncludes)
            {
                IQueryable previousQueryPointer = overwriteQueryable ?? _queryable;
                IEnumerable<object> previousNavigationsPointer = entities;

                foreach (var oneToOneNode in chain)
                {
                    var loadedNavigationsCurrentLevel = oneToOneNode.GetLoadedNavigationsNoType(previousNavigationsPointer);

                    var currentQuery = oneToOneNode.BuildNavigationQueryNoType(previousQueryPointer,
                        isUseJoin: IsCombineOneToOneQueryUsingEFInclude);

                    result.Add(new ManualIncludeQueryHelper.LoadedNavigationInfo
                    {
                        LastEntityType = oneToOneNode.LastEntityType,
                        LastNavigationType = oneToOneNode.LastNavigationType,
                        LastEntityOffsetFromFirstEntity = oneToOneNode.LastEntityOffsetFromFirstEntity,
                        FKName = oneToOneNode.FKName,
                        FKNameChain = oneToOneNode.FKNameChain,
                        LoadedNavigations = loadedNavigationsCurrentLevel.ToList(),
                        CurrentQuery = currentQuery,
                    });

                    previousNavigationsPointer = loadedNavigationsCurrentLevel;
                    previousQueryPointer = currentQuery;
                }
            }

            return result;
        }

        private void IncludeAllNavigations(IEnumerable<TEntity> entities,
            IQueryable<TEntity> overwriteQueryable = null,
            IEnumerable<ManualIncludeQueryHelper.LoadedNavigationInfo> loadedEntityQueryOneToOneNavigationInfos = null)
        {
            if (entities == null || !entities.Any())
            {
                return;
            }

            var sourceQuery = overwriteQueryable ?? _queryable;

            var allIncludableChains = _cachedAllIncludableOrderedChains ?? BuildAllIncludableOrderedChains();

            var loadedNavigations = new List<ManualIncludeQueryHelper.LoadedNavigationInfo>();

            if (loadedEntityQueryOneToOneNavigationInfos != null && loadedEntityQueryOneToOneNavigationInfos.Any())
            {
                loadedNavigations.AddRange(loadedEntityQueryOneToOneNavigationInfos);
            }

            foreach (var chain in allIncludableChains)
            {
                IEnumerable<object> previousEntities = null;
                IQueryable previousQuery = null;

                foreach (var node in chain)
                {
                    var sameNavigationLoaded = loadedNavigations
                        .Where(x => x.LastNavigationType == node.LastNavigationType)
                        .Where(x => x.LastEntityType == node.LastEntityType)
                        .Where(x => x.FKNameChain == node.FKNameChain)
                        .Where(x => x.LastEntityOffsetFromFirstEntity == node.LastEntityOffsetFromFirstEntity)
                        .FirstOrDefault();

                    if (sameNavigationLoaded == null)
                    {
                        var otherLoadedNavigations = loadedNavigations
                            .Where(x => x.LastNavigationType == node.LastNavigationType)
                            .SelectMany(x => x.LoadedNavigations);

                        if (typeof(TEntity) == node.LastNavigationType)
                        {
                            otherLoadedNavigations = otherLoadedNavigations.Concat(entities);
                        }

                        var invokeResult = node.InvokeQueryNoType(previousEntities ?? entities,
                            this.IsCombineOneToOneQueryUsingEFInclude,
                            previousQuery ?? sourceQuery,
                            otherLoadedNavigations);

                        previousEntities = invokeResult.Navigations;
                        previousQuery = node.CachedNavigationQueryNoType;

                        loadedNavigations.AddRange(invokeResult.LoadedNavigations);
                    }
                    else
                    {
                        var loadedEntities = sameNavigationLoaded.LoadedNavigations;
                        previousEntities = loadedEntities;
                        previousQuery = sameNavigationLoaded.CurrentQuery;
                    }
                }
            }
        }
    }

    public interface IOrderedManualIncludableQueryable<TEntity> : IManualIncludableQueryable<TEntity>
        where TEntity : class
    {
        IOrderedQueryable<TEntity> GetOrderedQueryable();

        IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable<TEntity> newOrderedQueryable);
    }

    public class OrderedManualIncludableQueryable<TEntity, TLastNavigation> : IOrderedManualIncludableQueryable<TEntity>
        where TEntity : class
        where TLastNavigation : class
    {
        private readonly IOrderedQueryable<TEntity> _orderedQueryable;

        private readonly ManualIncludableQueryable<TEntity, TLastNavigation> _manualIncludableQueryable;

        protected ManualIncludableQueryable<TEntity, TLastNavigation> ManualIncludableQueryable => _manualIncludableQueryable;

        public IOrderedQueryable<TEntity> GetOrderedQueryable()
        {
            return _orderedQueryable;
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return _orderedQueryable;
        }

        public OrderedManualIncludableQueryable(ManualIncludableQueryable<TEntity, TLastNavigation> manualIncludeQueryable,
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

            var query = new OrderedManualIncludableQueryable<TEntity, TLastNavigation>(newManualIncludableQueryable, newOrderedQueryable);

            return query;
        }

        public IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            return CreateNewOrderedQueryable(newOrderedQueryable);
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, IEnumerable<TNewNavigation>>> navigationPropertyPath)
        {
            return this.ManualIncludableQueryable.CreateNewOneToManyIncludeChainQuery(navigationPropertyPath);
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath)
        {
            return this.ManualIncludableQueryable.CreateNewOneToManyUniqueIncludeChainQuery(navigationPropertyPath);
        }

        ManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity>.CreateNewManyToOneIncludeChainQuery<TNewNavigation>(Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
             bool isOneToOne,
             bool isInvokeDistinctInMemory)
        {
            return this.ManualIncludableQueryable.CreateNewManyToOneIncludeChainQuery(navigationPropertyPath,
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
        internal static ReadOnlyCollection<KeySelector<TEntity>> GetEntityPksSelectorExpression<TEntity>(DbContext dbContext)
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var entityPks = entityType.FindPrimaryKey();

            var pksSelector = entityPks.Properties
                .Select(entityPk => new KeySelector<TEntity>
                {
                    LambdaExpression = GetPropertySelector<TEntity>(entityPk.Name),
                    UntypedGetter = BuildUntypedGetter<TEntity>(entityPk.PropertyInfo),
                })
                .ToList();

            var result = new ReadOnlyCollection<KeySelector<TEntity>>(pksSelector);

            return result;
        }

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
                //If it's the include bridge table the pk > 1, search pk by name
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
                var navigationPksFksMapping = navigationPks.Properties
                    .Select(x => new { PK = x, FKs = x.GetContainingForeignKeys() })
                    .ToList();

                var navigationPkCandidates = navigationPksFksMapping
                    .Where(x => x.FKs.Count() == 1 && x.FKs.Any(f => f.PrincipalEntityType == entityType
                        && f.PrincipalKey.Properties.Count == 1
                        && f.PrincipalKey.Properties.Single().Name == entityPk.Name
                        && f.Properties.Count == 1
                        && f.Properties.Single().Name == fkName))
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
                //use fk type which is nullable
                pkSelectorExpressionForJoin: GetPropertySelector<TEntity>(entityPk.Name, entityPk.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: entityPk.PropertyInfo.PropertyType,
                fkSelector: fkSelector,
                fkSelectorExpression: GetPropertySelector<TNavigation>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
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

            var isFKNullable = IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

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
                isNullableFk: isFKNullable,
                pKSelector: pkValueSelector,
                //use fk type which is nullable
                pkSelectorExpressionForJoin: GetPropertySelector<TNavigation>(pkName, pkProperty.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: pkProperty.PropertyInfo.PropertyType,
                fKSelector: fkFastSelector,
                fkSelectorExpression: GetPropertySelector<TEntity>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
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
                //If include bridge table, the pk > 1, so search pk by name
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
                //If more than one PK, like the bridge table, use FK to search the navigation PK (linked)

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
                //use fk type which is nullable
                pkSelectorExpressionForJoin: GetPropertySelector<TEntity>(entityPk.Name, entityPk.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: entityPk.PropertyInfo.PropertyType,
                fkSelector: fkSelector,
                fkSelectorExpression: GetPropertySelector<TNavigation>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
                navigationPKInverseEntityFKSelector: navigationPkSelector,
                navigationPKSelectors: navigationPksSelector,
                dbContext: dbContext,
                isFKNullable: isFKNullable
            );

            return oneToManyIncludeQueryChain;
        }

        public static IncludedNavigationQueryChainNodeInvokeQueryResult<T> InvokeQueryCore<T>(IIncludedNavigationQueryChainNode<T> node,
            IQueryable<T> filteredNavigationQuery,
            IQueryable<T> originalNavigationQuery,
            bool isCombineOneToOneQueryUsingEFInclude,
            IEnumerable<T> loadedNavigationsFilteredForThisInclude,
            bool isAllNavigationsAlreadyLoaded)
            where T : class
        {
            if (filteredNavigationQuery == null)
            {
                throw new ArgumentNullException(nameof(filteredNavigationQuery));
            }

            if (originalNavigationQuery == null)
            {
                throw new ArgumentNullException(nameof(originalNavigationQuery));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var oneToOneNodesChain = new List<IIncludedNavigationQueryChainNode>();

            if (isCombineOneToOneQueryUsingEFInclude)
            {
                var pointer = (IIncludedNavigationQueryChainNode)node;

                while (pointer.NextNode != null && pointer.NextNode.IsOneToOne)
                {
                    oneToOneNodesChain.Add(pointer.NextNode);

                    pointer = pointer.NextNode;
                }
            }

            var hasLoadedNavigations = loadedNavigationsFilteredForThisInclude != null && loadedNavigationsFilteredForThisInclude.Any();

            var canCombineOneToOneIncludes = false;

            if (oneToOneNodesChain.Count > 0)
            {
                canCombineOneToOneIncludes = true;

                if (hasLoadedNavigations)
                {
                    canCombineOneToOneIncludes = CanCombineOneToOneIncludeForLoadedNavigations(loadedNavigationsFilteredForThisInclude, oneToOneNodesChain) ?? false;
                }
            }

            var result = new IncludedNavigationQueryChainNodeInvokeQueryResult<T>();

            if (hasLoadedNavigations)
            {
                result.Navigations.AddRange(loadedNavigationsFilteredForThisInclude);
            }

            var includeQuery = filteredNavigationQuery;

            if (canCombineOneToOneIncludes)
            {
                var navigationPath = GetIncludeChainNavigationPath(oneToOneNodesChain);

                includeQuery = includeQuery.Include(navigationPath);
            }

            if (!isAllNavigationsAlreadyLoaded)
            {
                var navigations = includeQuery.ToList();

                result.Navigations.AddRange(navigations);
            }

            result.LoadedNavigations.Add(new LoadedNavigationInfo
            {
                LastEntityType = node.LastEntityType,
                LastNavigationType = node.LastNavigationType,
                LastEntityOffsetFromFirstEntity = node.LastEntityOffsetFromFirstEntity,
                FKName = node.FKName,
                FKNameChain = node.FKNameChain,
                LoadedNavigations = result.Navigations,
                CurrentQuery = originalNavigationQuery,
            });

            if (canCombineOneToOneIncludes)
            {
                IQueryable previousQueryPointer = includeQuery;
                IEnumerable<object> previousNavigationsPointer = result.Navigations;

                foreach (var oneToOneNode in oneToOneNodesChain)
                {
                    var loadedNavigationsCurrentLevel = oneToOneNode.GetLoadedNavigationsNoType(previousNavigationsPointer);

                    var currentLevelQuery = oneToOneNode.BuildNavigationQueryNoType(previousQueryPointer,
                        isUseJoin: isCombineOneToOneQueryUsingEFInclude);

                    result.LoadedNavigations.Add(new LoadedNavigationInfo
                    {
                        LastEntityType = oneToOneNode.LastEntityType,
                        LastNavigationType = oneToOneNode.LastNavigationType,
                        LastEntityOffsetFromFirstEntity = oneToOneNode.LastEntityOffsetFromFirstEntity,
                        FKName = oneToOneNode.FKName,
                        FKNameChain = oneToOneNode.FKNameChain,
                        LoadedNavigations = loadedNavigationsCurrentLevel.ToList(),
                        CurrentQuery = currentLevelQuery,
                    });

                    previousNavigationsPointer = loadedNavigationsCurrentLevel;
                    previousQueryPointer = currentLevelQuery;
                }
            }

            return result;
        }

        public static string GetIncludeChainNavigationPath(List<IIncludedNavigationQueryChainNode> orderedIncludeChain)
        {
            if (orderedIncludeChain == null || !orderedIncludeChain.Any())
            {
                return null;
            }

            var stringBuilder = new StringBuilder();

            foreach (var item in orderedIncludeChain)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append(".");
                }

                stringBuilder.Append(item.NavigationPropertyName);
            }

            return stringBuilder.ToString();
        }

        private static bool? CanCombineOneToOneIncludeForLoadedNavigations(IEnumerable<object> loadedNavigations,
            List<IIncludedNavigationQueryChainNode> oneToOneNodesChain)
        {
            var hasNavigations = loadedNavigations != null && loadedNavigations.Any();

            if (!hasNavigations)
            {
                return null;
            }

            if (oneToOneNodesChain.Count == 0)
            {
                return null;
            }

            IEnumerable<object> navigationsPointer = loadedNavigations;

            bool? isAllLevelOneToOneNavigationsFullyLoaded = null;

            foreach (var oneToOneNode in oneToOneNodesChain)
            {
                var isCurrentLevelFullyLoaded = oneToOneNode.IsAllNavigationsLoadedNoType(navigationsPointer);

                if (isCurrentLevelFullyLoaded.HasValue)
                {
                    isAllLevelOneToOneNavigationsFullyLoaded = isAllLevelOneToOneNavigationsFullyLoaded.HasValue
                        ? isAllLevelOneToOneNavigationsFullyLoaded.Value && isCurrentLevelFullyLoaded.Value
                        : isCurrentLevelFullyLoaded.Value;
                }

                if (isAllLevelOneToOneNavigationsFullyLoaded == false)
                {
                    break;
                }

                navigationsPointer = oneToOneNode.GetLoadedNavigationsNoType(navigationsPointer);
            }

            if (isAllLevelOneToOneNavigationsFullyLoaded == true)
            {
                return true;
            }

            return false;
        }

        public static List<IIncludedNavigationQueryChainNode> GetOrderedIIncludedNavigationQueryChainFromLastNode(IIncludedNavigationQueryChainNode node)
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

            return chain;
        }

        public static string BuildIIncludedNavigationQueryChainNodeFKNameChain(IIncludedNavigationQueryChainNode node)
        {
            if (node == null)
            {
                return null;
            }

            var stringBuilder = new StringBuilder();

            if (node.PreviousNode != null)
            {
                stringBuilder.Append(node.PreviousNode.FKNameChain);
                stringBuilder.Append(".");
            }

            stringBuilder.Append(node.FKName);

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

        public static IQueryable<TNavigation> BuildJoinQuerySelectInner<TEntity, TNavigation>(IQueryable<TEntity> outerQuery,
            IQueryable<TNavigation> interQuery,
            LambdaExpression outerSelector,
            LambdaExpression innerSelector,
            Type selectorKeyType)
        {
            var joinMethod = typeof(Queryable).GetMethods().Single(
                    method => method.Name == "Join"
                            && method.IsGenericMethodDefinition
                            && method.GetGenericArguments().Length == 4
                            && method.GetParameters().Length == 5);

            Expression<Func<TEntity, TNavigation, TNavigation>> resultSelector = (left, right) => right;

            object result = joinMethod
                    .MakeGenericMethod(typeof(TEntity), typeof(TNavigation), selectorKeyType, typeof(TNavigation))
                    .Invoke(null, new object[] { outerQuery, interQuery, outerSelector, innerSelector, resultSelector });

            var resultQuery = result as IQueryable<TNavigation>;

            return resultQuery;
        }

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

        internal class KeySelector<TEntity>
        {
            public LambdaExpression LambdaExpression { get; set; }

            public Func<TEntity, object> UntypedGetter { get; set; }
        }

        internal class LoadedNavigationInfo
        {
            public Type LastEntityType { get; set; }
            public Type LastNavigationType { get; set; }

            public string FKName { get; set; }

            public string FKNameChain { get; set; }

            public int LastEntityOffsetFromFirstEntity { get; set; }

            public IEnumerable<object> LoadedNavigations { get; set; } = new List<object>();

            public IQueryable CurrentQuery { get; set; }
        }

        internal class IncludedNavigationQueryChainNodeInvokeQueryResult<TNavigation>
            where TNavigation : class
        {
            public List<TNavigation> Navigations { get; set; } = new List<TNavigation>();

            public List<ManualIncludeQueryHelper.LoadedNavigationInfo> LoadedNavigations { get; set; } = new List<ManualIncludeQueryHelper.LoadedNavigationInfo>();
        }

        internal class IncludedNavigationQueryChainNodeInvokeQueryResultNoType
        {
            public IEnumerable<object> Navigations { get; set; } = new List<object>();

            public List<ManualIncludeQueryHelper.LoadedNavigationInfo> LoadedNavigations { get; set; } = new List<ManualIncludeQueryHelper.LoadedNavigationInfo>();
        }

        internal class BuildQueryWithAllOneToOneIncludesResult<TEntity>
        {
            public IQueryable<TEntity> EntityQueryaleWithOneToOneIncludes { get; set; }

            public List<List<IIncludedNavigationQueryChainNode>> AllOneToOneAutoIncludes { get; set; } = new List<List<IIncludedNavigationQueryChainNode>>();
        }
    }
}
