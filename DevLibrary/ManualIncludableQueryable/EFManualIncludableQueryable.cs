﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ManualIncludableQueryable
{
    #region node (single chain)

    internal class EFOneToManyIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        private readonly bool _isReGenerateNavigationQueryByPkOrFk;

        bool IIncludedNavigationQueryChainNode.IsReGenerateNavigationQueryByPkOrFk => _isReGenerateNavigationQueryByPkOrFk;

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

            IQueryable<TLastNavigation> navigationQuery;

            if (isUseJoin)
            {
                var navigationRawQuery = _dbContext.Set<TLastNavigation>().AsQueryable();

                if (!sourceQuery.GetIsTracking(_dbContext))
                {
                    navigationRawQuery = navigationRawQuery.AsNoTracking();
                }

                navigationQuery = ManualIncludableQueryableHelper.BuildJoinQueryToSelectOneToManyNavigation(sourceQuery,
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

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin: isUseJoin);
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

        public EFOneToManyIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
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
            bool isFKNullable,
            bool isReGenerateNavigationQueryByPkOrFk)
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
            _isReGenerateNavigationQueryByPkOrFk = isReGenerateNavigationQueryByPkOrFk;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = EFManualIncludableQueryableHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
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

            var isNeedToOverwriteQuery = hasLoadedNavigations
                || _isReGenerateNavigationQueryByPkOrFk
                //If take applied, we are not sure if has order by, of if the order by is good enough for join/select
                //(in some cases if join another table and select another table then the order changed, even you have order by clause, the then by can be changed)
                //So if source query has take then we force to use look up by id
                //It should not have performance problem since if you have take then it will not be too many entities
                || upperLevelQuery.HasAppliedTake();

            if (isNeedToOverwriteQuery)
            {
                var allFKKeys = entities.Select(_pkSelector).Distinct().ToList();

                var notLoadedFKKeys = new List<object>();

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

                    notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                    isAllNavigationsAlreadyLoaded = notLoadedFKKeys.Count == 0;

                    var loadedFKKeysForCurrentInclude = allFKKeys.Except(notLoadedFKKeys).ToList();

                    loadedNavigationsFilteredForThisInclude = loadedNavigations
                        .Where(x => loadedFKKeysForCurrentInclude.Contains(_fkSelector(x)))
                        .ToList();
                }
                else
                {
                    notLoadedFKKeys = allFKKeys.ToList();
                }

                var navigationFkSelector = ManualIncludableQueryableHelper.GetPropertySelector<TLastNavigation>(_fkName);
                var filterExpression = ManualIncludableQueryableHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                var navigationDbSetQuery = _dbContext.Set<TLastNavigation>()
                   .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    navigationDbSetQuery = navigationDbSetQuery.AsNoTracking();
                }

                query = Queryable.Where(navigationDbSetQuery, (dynamic)filterExpression);
            }

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var invokeQueryCoreResult = EFManualIncludableQueryableHelper.InvokeQueryCore(this,
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

            return result;
        }

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
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
    }

    internal class EFOneToManyUniqueIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        private readonly bool _isReGenerateNavigationQueryByPkOrFk;

        bool IIncludedNavigationQueryChainNode.IsReGenerateNavigationQueryByPkOrFk => _isReGenerateNavigationQueryByPkOrFk;

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

            IQueryable<TLastNavigation> navigationQuery;

            if (isUseJoin)
            {
                var navigationRawQuery = _dbContext.Set<TLastNavigation>().AsQueryable();

                if (!sourceQuery.GetIsTracking(_dbContext))
                {
                    navigationRawQuery = navigationRawQuery.AsNoTracking();
                }

                navigationQuery = ManualIncludableQueryableHelper.BuildJoinQueryToSelectOneToManyUniqueNavigation(sourceQuery,
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

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin: isUseJoin);
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

        public EFOneToManyUniqueIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
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
            bool isFKNullable,
            bool isReGenerateNavigationQueryByPkOrFk)
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
            _isReGenerateNavigationQueryByPkOrFk = isReGenerateNavigationQueryByPkOrFk;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = EFManualIncludableQueryableHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(upperLevelQuery,
                isUseJoin: isCombineOneToOneQueryUsingEFInclude);
            var query = navigationQuery;

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            var loadedNavigationsFilteredForThisInclude = new List<TLastNavigation>();

            var isAllNavigationsAlreadyLoaded = false;

            var isNeedToOverwriteQuery = hasLoadedNavigations
                || _isReGenerateNavigationQueryByPkOrFk
                //If take applied, we are not sure if has order by, of if the order by is good enough for join/select
                //(in some cases if join another table and select another table then the order changed, even you have order by clause, the then by can be changed)
                //So if source query has take then we force to use look up by id
                //It should not have performance problem since if you have take then it will not be too many entities
                || upperLevelQuery.HasAppliedTake();

            if (isNeedToOverwriteQuery)
            {
                var allFKKeys = entities.Select(_pkSelector).Distinct().ToList();

                var notLoadedFKKeys = new List<object>();

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

                    loadedNavigationsFilteredForThisInclude.AddRange(loadedNavigations.Where(x => allFKKeys.Contains(_fkSelector(x))));

                    notLoadedFKKeys = allFKKeys.Except(loadedFKKeys).ToList();

                    isAllNavigationsAlreadyLoaded = notLoadedFKKeys.Count == 0;
                }
                else
                {
                    notLoadedFKKeys = allFKKeys.ToList();
                }

                var navigationFkSelector = ManualIncludableQueryableHelper.GetPropertySelector<TLastNavigation>(_fkName);
                var filterExpression = ManualIncludableQueryableHelper.ConvertToContainsExpr(navigationFkSelector, notLoadedFKKeys);

                var navigationDbSetQuery = _dbContext.Set<TLastNavigation>()
                   .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    navigationDbSetQuery = navigationDbSetQuery.AsNoTracking();
                }

                query = Queryable.Where(navigationDbSetQuery, (dynamic)filterExpression);
            }

            var invokeQueryCoreResult = EFManualIncludableQueryableHelper.InvokeQueryCore(this,
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

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
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
    }

    internal class EFManyToOneIncludeQueryChainNode<TLastEntity, TLastNavigation> : IIncludedNavigationQueryChainNode<TLastEntity, TLastNavigation>
        where TLastEntity : class
        where TLastNavigation : class
    {
        private readonly IIncludedNavigationQueryChainNode _previousNode;

        private readonly bool _isReGenerateNavigationQueryByPkOrFk;

        bool IIncludedNavigationQueryChainNode.IsReGenerateNavigationQueryByPkOrFk => _isReGenerateNavigationQueryByPkOrFk;

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

            if (ManualIncludableQueryableHelper.IsNullableType(_navigationForeignKeyPropertyInfo.PropertyType))
            {
                var filterPropertyExpression = ManualIncludableQueryableHelper.GetPropertySelector<TLastEntity>(_fkName);
                var filterExpression = ManualIncludableQueryableHelper.ConvertToNotEqualsExpr(filterPropertyExpression, null);

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

                navigationQuery = ManualIncludableQueryableHelper.BuildJoinQueryToSelectManyToOneNavigation(sourceQueryFiltered,
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

            return BuildNavigationQuery(sourceQuery as IQueryable<TLastEntity>, isUseJoin: isUseJoin);
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

        public EFManyToOneIncludeQueryChainNode(IIncludedNavigationQueryChainNode previousNode,
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
            bool isReGenerateNavigationQueryByPkOrFk)
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
            _isReGenerateNavigationQueryByPkOrFk = isReGenerateNavigationQueryByPkOrFk;

            if (previousNode != null)
            {
                previousNode.AppendNextNode(this);
            }

            _fkNameChain = EFManualIncludableQueryableHelper.BuildIIncludedNavigationQueryChainNodeFKNameChain(this);
        }

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation> InvokeQuery(IEnumerable<TLastEntity> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable<TLastEntity> upperLevelQuery,
            IEnumerable<TLastNavigation> loadedNavigations = null)
        {
            var navigationQuery = BuildNavigationQuery(upperLevelQuery,
                isUseJoin: isCombineOneToOneQueryUsingEFInclude);
            var query = navigationQuery;

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<TLastNavigation>();

            var hasLoadedNavigations = loadedNavigations != null && loadedNavigations.Any();

            var loadedNavigationsFilteredForThisInclude = new List<TLastNavigation>();

            //If has too many loaded navigations the query performance is bad, so if has loaded navigation we force re-write query
            var isNeedToOverwriteQuery = hasLoadedNavigations
                || _isReGenerateNavigationQueryByPkOrFk
                //If take applied, we are not sure if has order by, of if the order by is good enough for join/select
                //(in some cases if join another table and select another table then the order changed, even you have order by clause, the then by can be changed)
                //So if source query has take then we force to use look up by id
                //It should not have performance problem since if you have take then it will not be too many entities
                || upperLevelQuery.HasAppliedTake();

            var isAllNavigationsAlreadyLoaded = false;

            if (isNeedToOverwriteQuery)
            {
                var loadedPkKeys = new List<object>();

                var notLoadedPkKeys = new List<object>();

                var allPkKeyValues = new List<object>();

                var allKeyValueEnumerable = entities.Select(_fkSelector);

                if (_isNullableFk)
                {
                    allKeyValueEnumerable = allKeyValueEnumerable.Where(x => x != null);
                }

                if (!_isOneToOne)
                {
                    allKeyValueEnumerable = allKeyValueEnumerable.Distinct();
                }

                allPkKeyValues = allKeyValueEnumerable.ToList();

                if (allPkKeyValues.Count == 0)
                {
                    //in case all FKs are null no need to load navigations
                    return result;
                }

                if (hasLoadedNavigations)
                {
                    loadedPkKeys = loadedNavigations
                        .Select(x => _pkSelector(x))
                        .Distinct()
                        .ToList();

                    loadedNavigationsFilteredForThisInclude = loadedNavigations
                        .Where(x => allPkKeyValues.Contains(_pkSelector(x)))
                        .ToList();

                    notLoadedPkKeys = allPkKeyValues.Except(loadedPkKeys).ToList();

                    isAllNavigationsAlreadyLoaded = notLoadedPkKeys.Count == 0;
                }
                else
                {
                    notLoadedPkKeys = allPkKeyValues.ToList();
                }

                var navigationPkSelector = ManualIncludableQueryableHelper.GetPropertySelector<TLastNavigation>(_pkName);

                var filterExpression = ManualIncludableQueryableHelper.ConvertToContainsExpr(navigationPkSelector, notLoadedPkKeys);

                var navigationDbSetQuery = _dbContext.Set<TLastNavigation>()
                    .AsQueryable();

                var isTracking = upperLevelQuery.GetIsTracking(_dbContext);

                if (!isTracking)
                {
                    navigationDbSetQuery = navigationDbSetQuery.AsNoTracking();
                }

                query = Queryable.Where(navigationDbSetQuery, (dynamic)filterExpression);
            }

            var invokeQueryCoreResult = EFManualIncludableQueryableHelper.InvokeQueryCore(this,
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

        public ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType InvokeQueryNoType(IEnumerable<object> entities,
            bool isCombineOneToOneQueryUsingEFInclude,
            IQueryable upperLevelQuery,
            IEnumerable<object> loadedNavigations = null)
        {
            var typedResult = InvokeQuery(entities?.Cast<TLastEntity>(),
                isCombineOneToOneQueryUsingEFInclude,
                upperLevelQuery as IQueryable<TLastEntity>,
                loadedNavigations?.Cast<TLastNavigation>());

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResultNoType
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
    }

    #endregion

    #region query (multiple chain)

    internal class EFManualIncludableQueryable<TEntity, TLastNavigation> : IManualIncludableQueryable<TEntity, TLastNavigation>
        where TEntity : class
        where
        TLastNavigation : class
    {
        public readonly bool IsCombineOneToOneQueryUsingEFInclude = true;

        private readonly IQueryable<TEntity> _queryable;

        public Expression Expression => _queryable.Expression;

        public Type ElementType => _queryable.ElementType;

        public IQueryProvider Provider => _queryable.Provider;

        protected IQueryable<TEntity> Queryable => _queryable;

        private readonly ReadOnlyCollection<EFManualIncludableQueryableHelper.KeySelector<TEntity>> _entityPksSelectorExpression;

        private readonly DbContext _dbContext;

        internal DbContext DbContext => _dbContext;

        protected Type LastNavigationEntityType { get; set; }

        public IQueryable<TEntity> GetQueryable()
        {
            return _queryable;
        }

        public EFManualIncludableQueryable(IQueryable<TEntity> queryable, DbContext dbContext)
        {
            _queryable = queryable;
            _dbContext = dbContext;

            _entityPksSelectorExpression = EFManualIncludableQueryableHelper.GetEntityPksSelectorExpression<TEntity>(dbContext);
        }

        private EFManualIncludableQueryable(IQueryable<TEntity> queryable,
            DbContext dbContext,
            ReadOnlyCollection<EFManualIncludableQueryableHelper.KeySelector<TEntity>> entityPksSelectorExpression)
        {
            _queryable = queryable;
            _dbContext = dbContext;
            _entityPksSelectorExpression = entityPksSelectorExpression;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return _queryable.GetEnumerator();
        }

        internal IIncludedNavigationQueryChainNode CurrentNode { get; set; }

        internal List<IIncludedNavigationQueryChainNode> QueryCompletedNodes { get; set; } = new List<IIncludedNavigationQueryChainNode>();

        public EFManualIncludableQueryable<TEntity, TLastNavigation> CreateNewReplaceQueryable(IQueryable<TEntity> newQueryable)
        {
            if (newQueryable == null)
            {
                throw new ArgumentNullException(nameof(newQueryable));
            }

            var query = new EFManualIncludableQueryable<TEntity, TLastNavigation>(newQueryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            return query;
        }

        IManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewReplaceQueryable(IQueryable newQueryable)
        {
            return this.CreateNewReplaceQueryable(newQueryable as IQueryable<TEntity>);
        }

        public EFOrderedManualIncludableQueryable<TEntity, TLastNavigation> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            if (newOrderedQueryable == null)
            {
                throw new ArgumentNullException(nameof(newOrderedQueryable));
            }

            var query = new EFManualIncludableQueryable<TEntity, TLastNavigation>(newOrderedQueryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());
            query.CurrentNode = this.CurrentNode;

            var newOrderedQuery = new EFOrderedManualIncludableQueryable<TEntity, TLastNavigation>(query, newOrderedQueryable);

            return newOrderedQuery;
        }

        IOrderedManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewOrderedQueryable(IOrderedQueryable newQueryable)
        {
            return this.CreateNewOrderedQueryable(newQueryable as IOrderedQueryable<TEntity>);
        }

        #region Then include

        public EFManualIncludableQueryable<TEntity, TNewNavigation> CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(Expression<Func<TPreviousNavigationEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TPreviousNavigationEntity : class
            where TNewNavigation : class
        {
            var manualIncludeType = EFManualIncludableQueryableHelper.GetManualIncludeType(this.LastNavigationEntityType, typeof(TNewNavigation), navigationPropertyPath, this.DbContext);

            switch (manualIncludeType)
            {
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToMany:
                    return CreateOneToManyThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(navigationPropertyPath,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.ManyToOne:
                    return CreateManyToOneThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(navigationPropertyPath,
                        isOneToOne: isOneToOne,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToManyUnique:
                    return CreateOneToManyUniqueThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(navigationPropertyPath,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                default:
                    throw new NotImplementedException($"Manual include type {manualIncludeType} not implemented");
            }
        }

        IManualIncludableQueryable<TEntity, TNewNavigation> IManualIncludableQueryable<TEntity, TLastNavigation>.CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(Expression<Func<TPreviousNavigationEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne,
            bool isInvokeDistinctInMemory)
            where TPreviousNavigationEntity : class
            where TNewNavigation : class
        {
            return this.CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(navigationPropertyPath,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isInvokeDistinctInMemory);
        }

        #region one-to-many

        public EFManualIncludableQueryable<TEntity, TNextNavigationCollection> CreateOneToManyThenIncludeQuery<TPreviousNavigationEntity, TNextNavigationCollection>(
            Expression<Func<TPreviousNavigationEntity, TNextNavigationCollection>> navigationPropertyPath,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TPreviousNavigationEntity : class
            where TNextNavigationCollection : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            if (LastNavigationEntityType == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(LastNavigationEntityType)));
            }

            if (typeof(TPreviousNavigationEntity) != LastNavigationEntityType)
            {
                throw new Exception("Then include doesn't apply", new ArgumentException("Previous entity type not match"));
            }

            var query = new EFManualIncludableQueryable<TEntity, TNextNavigationCollection>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            Type collectionElementType = typeof(TNextNavigationCollection).GetGenericArguments().First();

            Type targetType = typeof(IEnumerable<>).MakeGenericType(collectionElementType);

            LambdaExpression navigationPropertyPathConverted = navigationPropertyPath;

            if (targetType != typeof(TNextNavigationCollection))
            {
                Type delegateType = typeof(Func<,>).MakeGenericType(LastNavigationEntityType, targetType);

                var propertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);

                var parameter = Expression.Parameter(LastNavigationEntityType);
                var memberExpression = Expression.Property(parameter, propertyInfo.Name);

                navigationPropertyPathConverted = Expression.Lambda(delegateType, memberExpression, parameter);
            }

            object nodeObj = EFManualIncludableQueryableHelper.BuildOneToManyIncludeMethodInfo
                  .MakeGenericMethod(typeof(TPreviousNavigationEntity), collectionElementType)
                  .Invoke(null, new object[] { navigationPropertyPathConverted, this.DbContext, this.CurrentNode, isReGenerateNavigationQueryByPkOrFk });

            var node = nodeObj as IIncludedNavigationQueryChainNode;

            query.CurrentNode = node;
            query.LastNavigationEntityType = collectionElementType;

            return query;
        }

        #endregion

        #region one-to-many unique

        public EFManualIncludableQueryable<TEntity, TNextNavigation> CreateOneToManyUniqueThenIncludeQuery<TPreviousNavigationEntity, TNextNavigation>(
            Expression<Func<TPreviousNavigationEntity, TNextNavigation>> navigationPropertyPath,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TPreviousNavigationEntity : class
            where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            if (LastNavigationEntityType == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(LastNavigationEntityType)));
            }

            if (typeof(TPreviousNavigationEntity) != LastNavigationEntityType)
            {
                throw new Exception("Then include doesn't apply", new ArgumentException("Previous entity type not match"));
            }

            var query = new EFManualIncludableQueryable<TEntity, TNextNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            object nodeObj = EFManualIncludableQueryableHelper.BuildOneToManyUniqueIncludeMethodInfo
                 .MakeGenericMethod(typeof(TPreviousNavigationEntity), typeof(TNextNavigation))
                 .Invoke(null, new object[] { navigationPropertyPath, this.DbContext, this.CurrentNode, isReGenerateNavigationQueryByPkOrFk });

            var node = nodeObj as IIncludedNavigationQueryChainNode;

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNextNavigation);

            return query;
        }

        #endregion

        #region many-to-one

        public EFManualIncludableQueryable<TEntity, TNextNavigation> CreateManyToOneThenIncludeQuery<TPreviousNavigationEntity, TNextNavigation>(
            Expression<Func<TPreviousNavigationEntity, TNextNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNextNavigation : class
        {
            if (CurrentNode == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(CurrentNode)));
            }

            if (LastNavigationEntityType == null)
            {
                throw new Exception("Then include doesn't apply", new ArgumentNullException(nameof(LastNavigationEntityType)));
            }

            if (typeof(TPreviousNavigationEntity) != LastNavigationEntityType)
            {
                throw new Exception("Then include doesn't apply", new ArgumentException("Previous entity type not match"));
            }

            if (ManualIncludableQueryableHelper.IsIEnumerable(typeof(TNextNavigation)))
            {
                throw new ArgumentException(nameof(TNextNavigation));
            }

            var query = new EFManualIncludableQueryable<TEntity, TNextNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            object nodeObj = EFManualIncludableQueryableHelper.BuildManyToOneIncludeMethodInfo
                 .MakeGenericMethod(typeof(TPreviousNavigationEntity), typeof(TNextNavigation))
                 .Invoke(null, new object[] { navigationPropertyPath, this.DbContext, this.CurrentNode, isOneToOne, isReGenerateNavigationQueryByPkOrFk });

            var node = nodeObj as IIncludedNavigationQueryChainNode;

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNextNavigation);

            return query;
        }

        #endregion

        #endregion

        #region New chain

        public EFManualIncludableQueryable<TEntity, TNavigation> CreateNewIncludeChainQuery<TNavigation>(
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNavigation : class
        {
            var manualIncludeType = EFManualIncludableQueryableHelper.GetManualIncludeType(typeof(TEntity), typeof(TNavigation), navigationPropertyPath, this.DbContext);

            switch (manualIncludeType)
            {
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToMany:
                    return CreateNewOneToManyIncludeChainQueryUniverse(navigationPropertyPath,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.ManyToOne:
                    return CreateNewManyToOneIncludeChainQuery(navigationPropertyPath,
                        isOneToOne: isOneToOne,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToManyUnique:
                    return CreateNewOneToManyUniqueIncludeChainQuery(navigationPropertyPath,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                default:
                    throw new NotImplementedException($"Manual include type {manualIncludeType} not implemented");
            }
        }

        IManualIncludableQueryable<TEntity, TNavigation> IManualIncludableQueryable<TEntity>.CreateNewIncludeChainQuery<TNavigation>(
            LambdaExpression navigationPropertyPath,
           bool isOneToOne,
           bool isReGenerateNavigationQueryByPkOrFk)
           where TNavigation : class
        {
            return this.CreateNewIncludeChainQuery<TNavigation>(navigationPropertyPath: navigationPropertyPath as Expression<Func<TEntity, TNavigation>>,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
        }

        public EFManualIncludableQueryable<TEntity, TNewNavigationCollection> CreateNewOneToManyIncludeChainQueryUniverse<TNewNavigationCollection>(Expression<Func<TEntity, TNewNavigationCollection>> navigationPropertyPath,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNewNavigationCollection : class
        {
            var query = new EFManualIncludableQueryable<TEntity, TNewNavigationCollection>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            Type collectionElementType = typeof(TNewNavigationCollection).GetGenericArguments().First();

            Type targetType = typeof(IEnumerable<>).MakeGenericType(collectionElementType);

            LambdaExpression navigationPropertyPathConverted = navigationPropertyPath;

            if (targetType != typeof(TNewNavigationCollection))
            {
                Type delegateType = typeof(Func<,>).MakeGenericType(typeof(TEntity), targetType);

                var propertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);

                var parameter = Expression.Parameter(typeof(TEntity));
                var memberExpression = Expression.Property(parameter, propertyInfo.Name);

                navigationPropertyPathConverted = Expression.Lambda(delegateType, memberExpression, parameter);
            }

            object nodeObj = EFManualIncludableQueryableHelper.BuildOneToManyIncludeMethodInfo
                  .MakeGenericMethod(typeof(TEntity), collectionElementType)
                  .Invoke(null, new object[] { navigationPropertyPathConverted, this.DbContext, null, isReGenerateNavigationQueryByPkOrFk });

            var node = nodeObj as IIncludedNavigationQueryChainNode;

            query.CurrentNode = node;
            query.LastNavigationEntityType = collectionElementType;

            return query;
        }

        public EFManualIncludableQueryable<TEntity, TNewNavigation> CreateNewOneToManyUniqueIncludeChainQuery<TNewNavigation>(
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isReGenerateNavigationQueryByPkOrFk = false)
          where TNewNavigation : class
        {
            var query = new EFManualIncludableQueryable<TEntity, TNewNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = EFManualIncludableQueryableHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                this.DbContext,
                null,
                isReGenerateNavigationQueryByPkOrFk);

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNewNavigation);

            return query;
        }

        public EFManualIncludableQueryable<TEntity, TNewNavigation> CreateNewManyToOneIncludeChainQuery<TNewNavigation>(
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
           where TNewNavigation : class
        {
            if (ManualIncludableQueryableHelper.IsIEnumerable(typeof(TNewNavigation)))
            {
                throw new ArgumentException(nameof(TNewNavigation));
            }

            var query = new EFManualIncludableQueryable<TEntity, TNewNavigation>(this.Queryable, this.DbContext, this._entityPksSelectorExpression);

            query.QueryCompletedNodes.AddRange(this.QueryCompletedNodes.ToList());

            if (CurrentNode != null && !this.QueryCompletedNodes.Contains(CurrentNode))
            {
                query.QueryCompletedNodes.Add(CurrentNode);
            }

            var node = EFManualIncludableQueryableHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                this.DbContext,
                null,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNewNavigation);

            return query;
        }

        #endregion

        #region First Chain

        public static EFManualIncludableQueryable<TEntity, TNavigation> CreateFirstIncludeChainQuery<TNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNavigation : class
        {
            var manualIncludeType = EFManualIncludableQueryableHelper.GetManualIncludeType(typeof(TEntity), typeof(TNavigation), navigationPropertyPath, dbContext);

            switch (manualIncludeType)
            {
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToMany:
                    return EFManualIncludableQueryable<TEntity, TNavigation>.CreateFirstOneToManyIncludeChainQuery(queryable,
                        navigationPropertyPath,
                        dbContext,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.ManyToOne:
                    return EFManualIncludableQueryable<TEntity, TNavigation>.CreateFirstManyToOneIncludeChainQuery(queryable,
                        navigationPropertyPath,
                        dbContext,
                        isOneToOne: isOneToOne,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                case EFManualIncludableQueryableHelper.ManualIncludeType.OneToManyUnique:
                    return EFManualIncludableQueryable<TEntity, TNavigation>.CreateFirstOneToManyUniqueIncludeChainQuery(queryable,
                        navigationPropertyPath,
                        dbContext,
                        isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
                default:
                    throw new NotImplementedException($"Manual include type {manualIncludeType} not implemented");
            }
        }

        public static EFManualIncludableQueryable<TEntity, TNewNavigationCollection> CreateFirstOneToManyIncludeChainQuery<TNewNavigationCollection>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigationCollection>> navigationPropertyPath,
            DbContext dbContext,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNewNavigationCollection : class
        {
            var query = new EFManualIncludableQueryable<TEntity, TNewNavigationCollection>(queryable, dbContext);

            Type collectionElementType = typeof(TNewNavigationCollection).GetGenericArguments().First();

            Type targetType = typeof(IEnumerable<>).MakeGenericType(collectionElementType);

            LambdaExpression navigationPropertyPathConverted = navigationPropertyPath;

            if (targetType != typeof(TNewNavigationCollection))
            {
                Type delegateType = typeof(Func<,>).MakeGenericType(typeof(TEntity), targetType);

                var propertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);

                var parameter = Expression.Parameter(typeof(TEntity));
                var memberExpression = Expression.Property(parameter, propertyInfo.Name);

                navigationPropertyPathConverted = Expression.Lambda(delegateType, memberExpression, parameter);
            }

            object nodeObj = EFManualIncludableQueryableHelper.BuildOneToManyIncludeMethodInfo
                  .MakeGenericMethod(typeof(TEntity), collectionElementType)
                  .Invoke(null, new object[] { navigationPropertyPathConverted, dbContext, null, isReGenerateNavigationQueryByPkOrFk });

            var node = nodeObj as IIncludedNavigationQueryChainNode;

            query.CurrentNode = node;
            query.LastNavigationEntityType = collectionElementType;

            return query;
        }

        public static EFManualIncludableQueryable<TEntity, TNewNavigation> CreateFirstOneToManyUniqueIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNewNavigation : class
        {
            var query = new EFManualIncludableQueryable<TEntity, TNewNavigation>(queryable, dbContext);

            var node = EFManualIncludableQueryableHelper.BuildOneToManyUniqueInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null,
                isReGenerateNavigationQueryByPkOrFk);

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNewNavigation);

            return query;
        }

        public static EFManualIncludableQueryable<TEntity, TNewNavigation> CreateFirstManyToOneIncludeChainQuery<TNewNavigation>(IQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNewNavigation : class
        {
            if (ManualIncludableQueryableHelper.IsIEnumerable(typeof(TNewNavigation)))
            {
                throw new ArgumentException(nameof(TNewNavigation));
            }

            var query = new EFManualIncludableQueryable<TEntity, TNewNavigation>(queryable, dbContext);

            var node = EFManualIncludableQueryableHelper.BuildManyToOneInclude<TEntity, TNewNavigation>(
                navigationPropertyPath,
                dbContext,
                null,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);

            query.CurrentNode = node;
            query.LastNavigationEntityType = typeof(TNewNavigation);

            return query;
        }

        #endregion

        public static EFManualIncludableQueryable<TEntity, TEntity> CreateEmptyManualIncludableQueryable(IQueryable<TEntity> queryable, DbContext dbContext)
        {
            var query = new EFManualIncludableQueryable<TEntity, TEntity>(queryable, dbContext);

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

        dynamic IManualIncludableQueryable<TEntity>.InvokeQueryToList()
        {
            return this.InvokeQueryToList();
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

        dynamic IManualIncludableQueryable<TEntity>.InvokeQueryToArray()
        {
            return this.InvokeQueryToArray();
        }

        public TEntity InvokeQueryFirstOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.FirstOrDefault());
        }

        public TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.FirstOrDefault(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryFirstOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQueryFirstOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryFirst()
        {
            return InvokeQueryTakeOneCore(x => x.First());
        }

        public TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.First(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryFirst(LambdaExpression predicate)
        {
            return this.InvokeQueryFirst(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryLastOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.LastOrDefault());
        }

        public TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.LastOrDefault(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryLastOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQueryLastOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryLast()
        {
            return InvokeQueryTakeOneCore(x => x.Last());
        }

        public TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.Last(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryLast(LambdaExpression predicate)
        {
            return this.InvokeQueryLast(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQuerySingleOrDefault()
        {
            return InvokeQueryTakeOneCore(x => x.SingleOrDefault());
        }

        public TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.SingleOrDefault(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQuerySingleOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQuerySingleOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQuerySingle()
        {
            return InvokeQueryTakeOneCore(x => x.Single());
        }

        public TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate)
        {
            return InvokeQueryTakeOneCore(x => x.Single(predicate));
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQuerySingle(LambdaExpression predicate)
        {
            return this.InvokeQuerySingle(predicate as Expression<Func<TEntity, bool>>);
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

                var filterExpression = ManualIncludableQueryableHelper.ConvertToEqualsExpr(pkSelector.LambdaExpression, keyValue);

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
                .Select(x => EFManualIncludableQueryableHelper.GetOrderedIIncludedNavigationQueryChainFromLastNode(x))
                .ToList();

            _cachedAllIncludableOrderedChains = allIncludableChains;

            return allIncludableChains;
        }

        private EFManualIncludableQueryableHelper.BuildQueryWithAllOneToOneIncludesResult<TEntity> BuildEntityQueryWithAllOneToOneIncludes(IQueryable<TEntity> source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new EFManualIncludableQueryableHelper.BuildQueryWithAllOneToOneIncludesResult<TEntity>
            {
                EntityQueryaleWithOneToOneIncludes = source,
            };

            if (!IsCombineOneToOneQueryUsingEFInclude)
            {
                return result;
            }

            /*
             * Note: entity query with all one-to-one includes cannot filter out duplicated entities (if not tracking)
             */

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

                    var navigationPath = EFManualIncludableQueryableHelper.GetIncludeChainNavigationPath(oneToOneNodesChain);

                    query = query.Include(navigationPath);
                }
            }

            result.EntityQueryaleWithOneToOneIncludes = query;

            return result;
        }

        private List<ManualIncludableQueryableHelper.LoadedNavigationInfo> GetEntityQueryLoadedneToOneNavigationInfos(IEnumerable<TEntity> entities,
            List<List<IIncludedNavigationQueryChainNode>> allOneToOneAutoIncludes,
            IQueryable<TEntity> overwriteQueryable = null)
        {
            var result = new List<ManualIncludableQueryableHelper.LoadedNavigationInfo>();

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

                    result.Add(new ManualIncludableQueryableHelper.LoadedNavigationInfo
                    {
                        LastEntityType = oneToOneNode.LastEntityType,
                        LastNavigationType = oneToOneNode.LastNavigationType,
                        LastEntityOffsetFromFirstEntity = oneToOneNode.LastEntityOffsetFromFirstEntity,
                        FKName = oneToOneNode.FKName,
                        FKNameChain = oneToOneNode.FKNameChain,
                        NavigationPropertyName = oneToOneNode.NavigationPropertyName,
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
            IEnumerable<ManualIncludableQueryableHelper.LoadedNavigationInfo> loadedEntityQueryOneToOneNavigationInfos = null)
        {
            if (entities == null || !entities.Any())
            {
                return;
            }

            var sourceQuery = overwriteQueryable ?? _queryable;

            var allIncludableChains = _cachedAllIncludableOrderedChains ?? BuildAllIncludableOrderedChains();

            var loadedNavigations = new List<ManualIncludableQueryableHelper.LoadedNavigationInfo>();

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
                        .Where(x => x.LastEntityType == node.LastEntityType)
                        .Where(x => x.FKNameChain == node.FKNameChain)
                        //For self-table reference
                        .Where(x => x.NavigationPropertyName == node.NavigationPropertyName)
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

    internal class EFOrderedManualIncludableQueryable<TEntity, TLastNavigation> : IOrderedManualIncludableQueryable<TEntity, TLastNavigation>
        where TEntity : class
        where TLastNavigation : class
    {
        private readonly IOrderedQueryable<TEntity> _orderedQueryable;

        private readonly EFManualIncludableQueryable<TEntity, TLastNavigation> _manualIncludableQueryable;

        protected EFManualIncludableQueryable<TEntity, TLastNavigation> ManualIncludableQueryable => _manualIncludableQueryable;

        public IOrderedQueryable<TEntity> GetOrderedQueryable()
        {
            return _orderedQueryable;
        }

        public IQueryable<TEntity> GetQueryable()
        {
            return _orderedQueryable;
        }

        public EFOrderedManualIncludableQueryable(EFManualIncludableQueryable<TEntity, TLastNavigation> manualIncludeQueryable,
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

        IManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewReplaceQueryable(IQueryable newQueryable)
        {
            return this.CreateNewReplaceQueryable(newQueryable as IQueryable<TEntity>);
        }

        public IOrderedManualIncludableQueryable<TEntity> CreateNewOrderedQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            if (newOrderedQueryable == null)
            {
                throw new ArgumentNullException(nameof(newOrderedQueryable));
            }

            var newManualIncludableQueryable = this.ManualIncludableQueryable.CreateNewReplaceQueryable(newOrderedQueryable);

            var query = new EFOrderedManualIncludableQueryable<TEntity, TLastNavigation>(newManualIncludableQueryable, newOrderedQueryable);

            return query;
        }

        IOrderedManualIncludableQueryable<TEntity> IManualIncludableQueryable<TEntity>.CreateNewOrderedQueryable(IOrderedQueryable newQueryable)
        {
            return this.CreateNewOrderedQueryable(newQueryable as IOrderedQueryable<TEntity>);
        }

        public IOrderedManualIncludableQueryable<TEntity> CreateNewReplaceOrdredQueryable(IOrderedQueryable<TEntity> newOrderedQueryable)
        {
            return CreateNewOrderedQueryable(newOrderedQueryable);
        }

        IOrderedManualIncludableQueryable<TEntity> IOrderedManualIncludableQueryable<TEntity>.CreateNewReplaceOrdredQueryable(IOrderedQueryable newOrderedQueryable)
        {
            return this.CreateNewOrderedQueryable(newOrderedQueryable as IOrderedQueryable<TEntity>);
        }

        public static EFOrderedManualIncludableQueryable<TEntity, TEntity> CreateEmptyOrderedManualIncludableQueryable(IOrderedQueryable<TEntity> queryable, DbContext dbContext)
        {
            var manualIncludableQueryable = EFManualIncludableQueryable<TEntity, TEntity>.CreateEmptyManualIncludableQueryable(queryable, dbContext);

            var query = new EFOrderedManualIncludableQueryable<TEntity, TEntity>(manualIncludableQueryable, queryable);

            return query;
        }

        public static EFOrderedManualIncludableQueryable<TEntity, TNavigation> CreateFirstIncludeChainQuery<TNavigation>(IOrderedQueryable<TEntity> queryable,
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TNavigation : class
        {
            var manualIncludabeQueryable = EFManualIncludableQueryable<TEntity, TNavigation>.CreateFirstIncludeChainQuery(queryable,
                navigationPropertyPath,
                dbContext,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);

            return new EFOrderedManualIncludableQueryable<TEntity, TNavigation>(manualIncludabeQueryable, queryable);
        }

        public EFManualIncludableQueryable<TEntity, TNewNavigation> CreateNewIncludeChainQuery<TNewNavigation>(
            Expression<Func<TEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TNewNavigation : class
        {
            return this.ManualIncludableQueryable.CreateNewIncludeChainQuery(navigationPropertyPath,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
        }

        IManualIncludableQueryable<TEntity, TNavigation> IManualIncludableQueryable<TEntity>.CreateNewIncludeChainQuery<TNavigation>(
            LambdaExpression navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TNavigation : class
        {
            return this.CreateNewIncludeChainQuery<TNavigation>(navigationPropertyPath: navigationPropertyPath as Expression<Func<TEntity, TNavigation>>,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);
        }

        IOrderedManualIncludableQueryable<TEntity, TNavigation> IOrderedManualIncludableQueryable<TEntity>.CreateNewIncludeChainQuery<TNavigation>(
            LambdaExpression navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TNavigation : class
        {
            var newManualQueryable = this.CreateNewIncludeChainQuery<TNavigation>(navigationPropertyPath: navigationPropertyPath as Expression<Func<TEntity, TNavigation>>,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);

            return new EFOrderedManualIncludableQueryable<TEntity, TNavigation>(newManualQueryable, this._orderedQueryable);
        }

        IOrderedManualIncludableQueryable<TEntity, TNewNavigation> IOrderedManualIncludableQueryable<TEntity, TLastNavigation>.CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(Expression<Func<TPreviousNavigationEntity, TNewNavigation>> navigationPropertyPath,
            bool isOneToOne,
            bool isReGenerateNavigationQueryByPkOrFk)
            where TPreviousNavigationEntity : class
            where TNewNavigation : class
        {
            var newManualIncludeQueryable = this.ManualIncludableQueryable.CreateThenIncludeQuery<TPreviousNavigationEntity, TNewNavigation>(
                navigationPropertyPath,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk);

            return new EFOrderedManualIncludableQueryable<TEntity, TNewNavigation>(newManualIncludeQueryable, this._orderedQueryable);
        }

        public List<TEntity> InvokeQueryToList()
        {
            return _manualIncludableQueryable.InvokeQueryToList();
        }

        dynamic IManualIncludableQueryable<TEntity>.InvokeQueryToList()
        {
            return this.InvokeQueryToList();
        }

        public TEntity[] InvokeQueryToArray()
        {
            return _manualIncludableQueryable.InvokeQueryToArray();
        }

        dynamic IManualIncludableQueryable<TEntity>.InvokeQueryToArray()
        {
            return this.InvokeQueryToArray();
        }

        public TEntity InvokeQueryFirstOrDefault()
        {
            return _manualIncludableQueryable.InvokeQueryFirstOrDefault();
        }

        public TEntity InvokeQueryFirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryFirstOrDefault(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryFirstOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQueryFirstOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryFirst()
        {
            return _manualIncludableQueryable.InvokeQueryFirst();
        }

        public TEntity InvokeQueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryFirst(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryFirst(LambdaExpression predicate)
        {
            return this.InvokeQueryFirst(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryLastOrDefault()
        {
            return _manualIncludableQueryable.InvokeQueryLastOrDefault();
        }

        public TEntity InvokeQueryLastOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryLastOrDefault(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryLastOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQueryLastOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQueryLast()
        {
            return _manualIncludableQueryable.InvokeQueryLast();
        }

        public TEntity InvokeQueryLast(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQueryLast(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQueryLast(LambdaExpression predicate)
        {
            return this.InvokeQueryLast(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQuerySingleOrDefault()
        {
            return _manualIncludableQueryable.InvokeQuerySingleOrDefault();
        }

        public TEntity InvokeQuerySingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQuerySingleOrDefault(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQuerySingleOrDefault(LambdaExpression predicate)
        {
            return this.InvokeQuerySingleOrDefault(predicate as Expression<Func<TEntity, bool>>);
        }

        public TEntity InvokeQuerySingle()
        {
            return _manualIncludableQueryable.InvokeQuerySingle();
        }

        public TEntity InvokeQuerySingle(Expression<Func<TEntity, bool>> predicate)
        {
            return _manualIncludableQueryable.InvokeQuerySingle(predicate);
        }

        TEntity IManualIncludableQueryable<TEntity>.InvokeQuerySingle(LambdaExpression predicate)
        {
            return this.InvokeQuerySingle(predicate as Expression<Func<TEntity, bool>>);
        }
    }

    #endregion

    internal static class EFManualIncludableQueryableHelper
    {
        public enum ManualIncludeType
        {
            OneToMany,
            ManyToOne,
            OneToManyUnique,
        }

        internal static ReadOnlyCollection<KeySelector<TEntity>> GetEntityPksSelectorExpression<TEntity>(DbContext dbContext)
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var entityPks = entityType.FindPrimaryKey();

            var pksSelector = entityPks.Properties
                .Select(entityPk => new KeySelector<TEntity>
                {
                    LambdaExpression = ManualIncludableQueryableHelper.GetPropertySelector<TEntity>(entityPk.Name),
                    UntypedGetter = ManualIncludableQueryableHelper.BuildUntypedGetter<TEntity>(entityPk.PropertyInfo),
                })
                .ToList();

            var result = new ReadOnlyCollection<KeySelector<TEntity>>(pksSelector);

            return result;
        }

        public static readonly MethodInfo BuildOneToManyIncludeMethodInfo = typeof(EFManualIncludableQueryableHelper)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(BuildOneToManyInclude))
            .Single();

        public static EFOneToManyIncludeQueryChainNode<TEntity, TNavigation> BuildOneToManyInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, IEnumerable<TNavigation>>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);
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

            var isFKNullable = ManualIncludableQueryableHelper.IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var fkName = navigationForeignKeyProperty.Name;

            var navigationPropertySelector = ManualIncludableQueryableHelper.GetPropertySelector<TEntity, IEnumerable<TNavigation>>(navigationPropertyInfo.Name);

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

            var pkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var fkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationType = dbContext.Model.FindEntityType(typeof(TNavigation).FullName);

            var navigationPks = navigationType.FindPrimaryKey();

            Microsoft.EntityFrameworkCore.Metadata.IProperty navigationPk = null;

            if (navigationPks.Properties.Count == 1)
            {
                navigationPk = navigationPks.Properties.Single();
            }
            else
            {
                //If more than one PK, for now it must be the bridge table
                //So use FK to search the navigation PK (linked)

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

            var navigationPkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(navigationPk.PropertyInfo);

            var navigationPksSelector = navigationPks.Properties
                .Select(x => ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(x.PropertyInfo))
                .ToList();

            var oneToManyIncludeQueryChain = new EFOneToManyIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationInversePkPropertyInfo: inversePkNavigationPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: ManualIncludableQueryableHelper.BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                navigationInversePkPropertyInfoSetter: ManualIncludableQueryableHelper.BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo),
                fkName: fkName,
                pkSelector: pkSelector,
                //use fk type which is nullable
                pkSelectorExpressionForJoin: ManualIncludableQueryableHelper.GetPropertySelector<TEntity>(entityPk.Name, entityPk.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: entityPk.PropertyInfo.PropertyType,
                fkSelector: fkSelector,
                fkSelectorExpression: ManualIncludableQueryableHelper.GetPropertySelector<TNavigation>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
                navigationPKInverseEntityFKSelector: navigationPkSelector,
                navigationPKSelectors: navigationPksSelector,
                dbContext: dbContext,
                isFKNullable: isFKNullable,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk
            );

            return oneToManyIncludeQueryChain;
        }

        public static readonly MethodInfo BuildManyToOneIncludeMethodInfo = typeof(EFManualIncludableQueryableHelper)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(BuildManyToOneInclude))
            .Single();

        public static EFManyToOneIncludeQueryChainNode<TEntity, TNavigation> BuildManyToOneInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode,
            bool isOneToOne = false,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);
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

            var navigationPropertySelector = ManualIncludableQueryableHelper.GetPropertySelector<TEntity, TNavigation>(navigationPropertyInfo.Name);

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

            var pkValueSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(pkProperty.PropertyInfo);
            var fkFastSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TEntity>(navigationForeignKeyPropertyInfo);

            var isFKNullable = ManualIncludableQueryableHelper.IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var manyToOneIncludeQueryChain = new EFManyToOneIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationForeignKeyPropertyInfo: navigationForeignKeyPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: ManualIncludableQueryableHelper.BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                pkName: pkName,
                fkName: fkName,
                isNullableFk: isFKNullable,
                pKSelector: pkValueSelector,
                //use fk type which is nullable
                pkSelectorExpressionForJoin: ManualIncludableQueryableHelper.GetPropertySelector<TNavigation>(pkName, pkProperty.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: pkProperty.PropertyInfo.PropertyType,
                fKSelector: fkFastSelector,
                fkSelectorExpression: ManualIncludableQueryableHelper.GetPropertySelector<TEntity>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
                dbContext: dbContext,
                isOneToOne: isOneToOne,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk
            );

            return manyToOneIncludeQueryChain;
        }

        public static readonly MethodInfo BuildOneToManyUniqueIncludeMethodInfo = typeof(EFManualIncludableQueryableHelper)
            .GetTypeInfo()
            .GetDeclaredMethods(nameof(BuildOneToManyUniqueInclude))
            .Single();

        public static EFOneToManyUniqueIncludeQueryChainNode<TEntity, TNavigation> BuildOneToManyUniqueInclude<TEntity, TNavigation>(
            Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext,
            IIncludedNavigationQueryChainNode previousNode,
            bool isReGenerateNavigationQueryByPkOrFk = false)
            where TEntity : class
            where TNavigation : class
        {
            var entityType = dbContext.Model.FindEntityType(typeof(TEntity).FullName);

            var navigationPropertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);
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

            var isFKNullable = ManualIncludableQueryableHelper.IsNullableType(navigationForeignKeyPropertyInfo.PropertyType);

            var fkName = navigationForeignKeyProperty.Name;

            var navigationPropertySelector = ManualIncludableQueryableHelper.GetPropertySelector<TEntity, TNavigation>(navigationPropertyInfo.Name);

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

            var pkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TEntity>(entityPk.PropertyInfo);

            var fkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(navigationForeignKeyProperty.PropertyInfo);

            var navigationType = dbContext.Model.FindEntityType(typeof(TNavigation).FullName);

            var navigationPks = navigationType.FindPrimaryKey();

            Microsoft.EntityFrameworkCore.Metadata.IProperty navigationPk = null;

            if (navigationPks.Properties.Count == 1)
            {
                navigationPk = navigationPks.Properties.Single();
            }
            else
            {
                //If more than one PK, for now it must be the bridge table
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

            var navigationPkSelector = ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(navigationPk.PropertyInfo);

            var navigationPksSelector = navigationPks.Properties
                .Select(x => ManualIncludableQueryableHelper.BuildUntypedGetter<TNavigation>(x.PropertyInfo))
                .ToList();

            var oneToManyIncludeQueryChain = new EFOneToManyUniqueIncludeQueryChainNode<TEntity, TNavigation>
            (
                previousNode: previousNode,
                lastEntityOffsetFromFirstEntity: previousNode == null ? 1 : previousNode.LastEntityOffsetFromFirstEntity + 1,
                navigationPropertySelector: navigationPropertySelector,
                navigationPropertyInfo: navigationPropertyInfo,
                navigationInversePkPropertyInfo: inversePkNavigationPropertyInfo,
                navigationPropertyInfoGetter: navigationPropertyPath.Compile(),
                navigationPropertyInfoSetter: ManualIncludableQueryableHelper.BuildUntypedSetter<TEntity>(navigationPropertyInfo),
                navigationInversePkPropertyInfoSetter: ManualIncludableQueryableHelper.BuildUntypedSetter<TNavigation>(inversePkNavigationPropertyInfo),
                fkName: fkName,
                pkSelector: pkSelector,
                //use fk type which is nullable
                pkSelectorExpressionForJoin: ManualIncludableQueryableHelper.GetPropertySelector<TEntity>(entityPk.Name, entityPk.PropertyInfo.PropertyType, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                pkType: entityPk.PropertyInfo.PropertyType,
                fkSelector: fkSelector,
                fkSelectorExpression: ManualIncludableQueryableHelper.GetPropertySelector<TNavigation>(fkName, navigationForeignKeyProperty.PropertyInfo.PropertyType),
                fkType: navigationForeignKeyProperty.PropertyInfo.PropertyType,
                navigationPKInverseEntityFKSelector: navigationPkSelector,
                navigationPKSelectors: navigationPksSelector,
                dbContext: dbContext,
                isFKNullable: isFKNullable,
                isReGenerateNavigationQueryByPkOrFk: isReGenerateNavigationQueryByPkOrFk
            );

            return oneToManyIncludeQueryChain;
        }

        public static ManualIncludeType GetManualIncludeType<TEntity, TNavigation>(Expression<Func<TEntity, TNavigation>> navigationPropertyPath,
            DbContext dbContext)
            where TEntity : class
            where TNavigation : class
        {
            return GetManualIncludeType(typeof(TEntity), typeof(TNavigation), navigationPropertyPath, dbContext);
        }

        public static ManualIncludeType GetManualIncludeType(Type entityType,
            Type navigationType,
            LambdaExpression navigationPropertyPath,
            DbContext dbContext)
        {
            if (ManualIncludableQueryableHelper.IsIEnumerable(navigationType))
            {
                return ManualIncludeType.OneToMany;
            }

            var entityTypeDbContext = dbContext.Model.FindEntityType(entityType.FullName);

            var navigationPropertyInfo = ManualIncludableQueryableHelper.GetPropertyInfo(navigationPropertyPath);
            var navigation = entityTypeDbContext.FindNavigation(navigationPropertyInfo.Name);

            if (navigation == null)
            {
                throw new ArgumentException("Cannot find navigation property", nameof(navigationPropertyPath));
            }

            var navigationForeignKey = navigation.ForeignKey;

            var pkProperty = navigationForeignKey.PrincipalKey.Properties.Single();

            if (pkProperty.DeclaringEntityType.Name != navigationType.FullName)
            {
                return ManualIncludeType.OneToManyUnique;
            }

            return ManualIncludeType.ManyToOne;
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

        public static ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<T> InvokeQueryCore<T>(IIncludedNavigationQueryChainNode<T> node,
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

            var result = new ManualIncludableQueryableHelper.IncludedNavigationQueryChainNodeInvokeQueryResult<T>();

            if (hasLoadedNavigations)
            {
                result.Navigations.AddRange(loadedNavigationsFilteredForThisInclude);
            }

            var includeQuery = filteredNavigationQuery;

            if (canCombineOneToOneIncludes)
            {
                var navigationPath = EFManualIncludableQueryableHelper.GetIncludeChainNavigationPath(oneToOneNodesChain);

                includeQuery = includeQuery.Include(navigationPath);
            }

            if (!isAllNavigationsAlreadyLoaded)
            {
                var navigations = includeQuery.ToList();

                result.Navigations.AddRange(navigations);
            }

            result.LoadedNavigations.Add(new ManualIncludableQueryableHelper.LoadedNavigationInfo
            {
                LastEntityType = node.LastEntityType,
                LastNavigationType = node.LastNavigationType,
                LastEntityOffsetFromFirstEntity = node.LastEntityOffsetFromFirstEntity,
                FKName = node.FKName,
                FKNameChain = node.FKNameChain,
                NavigationPropertyName = node.NavigationPropertyName,
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

                    result.LoadedNavigations.Add(new ManualIncludableQueryableHelper.LoadedNavigationInfo
                    {
                        LastEntityType = oneToOneNode.LastEntityType,
                        LastNavigationType = oneToOneNode.LastNavigationType,
                        LastEntityOffsetFromFirstEntity = oneToOneNode.LastEntityOffsetFromFirstEntity,
                        FKName = oneToOneNode.FKName,
                        FKNameChain = oneToOneNode.FKNameChain,
                        NavigationPropertyName = oneToOneNode.NavigationPropertyName,
                        LoadedNavigations = loadedNavigationsCurrentLevel.ToList(),
                        CurrentQuery = currentLevelQuery,
                    });

                    previousNavigationsPointer = loadedNavigationsCurrentLevel;
                    previousQueryPointer = currentLevelQuery;
                }
            }

            return result;
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

            stringBuilder.Append($"{node.LastNavigationType.Name}_{node.FKName}");

            return stringBuilder.ToString();
        }

        internal class KeySelector<TEntity>
        {
            public LambdaExpression LambdaExpression { get; set; }

            public Func<TEntity, object> UntypedGetter { get; set; }
        }

        internal class BuildQueryWithAllOneToOneIncludesResult<TEntity>
        {
            public IQueryable<TEntity> EntityQueryaleWithOneToOneIncludes { get; set; }

            public List<List<IIncludedNavigationQueryChainNode>> AllOneToOneAutoIncludes { get; set; } = new List<List<IIncludedNavigationQueryChainNode>>();
        }
    }
}