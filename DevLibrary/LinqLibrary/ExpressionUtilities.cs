using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqLibrary
{
    public static class ExpressionUtilities
    {
        public static Expression<Func<TSource, TDestination>> Combine<TSource, TDestination>(params Expression<Func<TSource, TDestination>>[] selectors)
        {
            var param = Expression.Parameter(typeof(TSource), "x");

            var bindings = from selector in selectors
                           let replace = new ParameterReplaceVisitor(
                                 selector.Parameters[0], param)
                           from binding in ((MemberInitExpression)selector.Body).Bindings
                                 .OfType<MemberAssignment>()
                           select Expression.Bind(binding.Member,
                                 replace.VisitAndConvert(binding.Expression, "Combine"));

            return Expression.Lambda<Func<TSource, TDestination>>(
                Expression.MemberInit(
                    Expression.New(typeof(TDestination).GetConstructor(Type.EmptyTypes)),
                    bindings
                )
                , param);
        }

        public static Expression<Func<TSource, TDestination>> Combine<TSource, TDestination>(params SelectorBindingModel[] selectors)
        {
            var param = Expression.Parameter(typeof(TSource), "x");

            return Expression.Lambda<Func<TSource, TDestination>>(
                Expression.MemberInit(
                    Expression.New(typeof(TDestination).GetConstructor(Type.EmptyTypes)),
                    from selector in selectors
                    let replace = new ParameterReplaceVisitor(selector.SourceSelector.Parameters[0], param)
                    let memberInfo = typeof(TDestination).GetMember(selector.TargetBindingPropertyName).FirstOrDefault()
                    select Expression.Bind(memberInfo, replace.VisitAndConvert(selector.SourceSelector.Body, "Combine")))
                , param);
        }

        public static Expression<Func<TSource1, TSource2, TDestination>> CombineJoinSelector<TSource1, TSource2, TDestination>(IEnumerable<Expression<Func<TSource1, TDestination>>> selectors1,
            IEnumerable<Expression<Func<TSource2, TDestination>>> selectors2)
        {
            var param1 = Expression.Parameter(typeof(TSource1), "x1");
            var param2 = Expression.Parameter(typeof(TSource2), "x2");

            var bindings1 = from selector in selectors1
                            let replace = new ParameterReplaceVisitor(
                                  selector.Parameters[0], param1)
                            from binding in ((MemberInitExpression)selector.Body).Bindings
                                  .OfType<MemberAssignment>()
                            select Expression.Bind(binding.Member,
                                  replace.VisitAndConvert(binding.Expression, "Combine"));

            var bindings2 = from selector in selectors2
                            let replace = new ParameterReplaceVisitor(
                                  selector.Parameters[0], param2)
                            from binding in ((MemberInitExpression)selector.Body).Bindings
                                  .OfType<MemberAssignment>()
                            select Expression.Bind(binding.Member,
                                  replace.VisitAndConvert(binding.Expression, "Combine"));

            return Expression.Lambda<Func<TSource1, TSource2, TDestination>>(
                Expression.MemberInit(
                    Expression.New(typeof(TDestination).GetConstructor(Type.EmptyTypes)),
                    bindings1.Concat(bindings2)),
                param1,
                param2);
        }

        public static Expression<Func<TSource1, TSource2, TDestination>> CombineJoinSelector<TSource1, TSource2, TDestination>(Expression<Func<TSource1, TDestination>> selector1,
            Expression<Func<TSource2, TDestination>> selector2)
        {
            return CombineJoinSelector(new Expression<Func<TSource1, TDestination>>[] { selector1 }, new Expression<Func<TSource2, TDestination>>[] { selector2 });
        }

        public static Expression<Func<TSource1, TSource2, TDestination>> CombineJoinSelector<TSource1, TSource2, TDestination>(IEnumerable<SelectorBindingModel> selectors1,
            IEnumerable<SelectorBindingModel> selectors2)
        {
            var param1 = Expression.Parameter(typeof(TSource1), "x1");
            var param2 = Expression.Parameter(typeof(TSource2), "x2");

            var binders1 = from selector in selectors1 ?? new List<SelectorBindingModel>()
                           let replace = new ParameterReplaceVisitor(selector.SourceSelector.Parameters[0], param1)
                           let memberInfo = typeof(TDestination).GetMember(selector.TargetBindingPropertyName).FirstOrDefault()
                           select Expression.Bind(memberInfo, replace.VisitAndConvert(selector.SourceSelector.Body, "Combine"));

            var binders2 = from selector in selectors2 ?? new List<SelectorBindingModel>()
                           let replace = new ParameterReplaceVisitor(selector.SourceSelector.Parameters[0], param2)
                           let memberInfo = typeof(TDestination).GetMember(selector.TargetBindingPropertyName).FirstOrDefault()
                           select Expression.Bind(memberInfo, replace.VisitAndConvert(selector.SourceSelector.Body, "Combine"));

            return Expression.Lambda<Func<TSource1, TSource2, TDestination>>(
                Expression.MemberInit(
                    Expression.New(typeof(TDestination).GetConstructor(Type.EmptyTypes)),
                    binders1.Concat(binders2)),
                param1,
                param2);
        }

        public static LambdaExpression CombineJoinSelector(Type destinationType,
            params (Type SourceType, IEnumerable<SelectorBindingModel> Selectors)[] typeSelectors)
        {
            if (typeSelectors == null || !typeSelectors.Any())
            {
                //How to return empty selector?
                throw new NotImplementedException();
            }

            var typesCount = typeSelectors.Length;

            var parameterExpressions = new ParameterExpression[typesCount];
            var binders = new IEnumerable<MemberAssignment>[typesCount];

            for (int i = 0; i < typesCount; i++)
            {
                var paramName = $"x{i + 1}";

                var param = Expression.Parameter(typeSelectors[i].SourceType, paramName);

                parameterExpressions[i] = param;

                var bindersForType = from selector in typeSelectors[i].Selectors ?? new List<SelectorBindingModel>()
                                     let replace = new ParameterReplaceVisitor(selector.SourceSelector.Parameters[0], param)
                                     let memberInfo = destinationType.GetMember(selector.TargetBindingPropertyName).FirstOrDefault()
                                     select Expression.Bind(memberInfo, replace.VisitAndConvert(selector.SourceSelector.Body, "Combine"));

                binders[i] = bindersForType;
            }

            return Expression.Lambda(Expression.MemberInit(
                    Expression.New(destinationType.GetConstructor(Type.EmptyTypes)),
                    binders.SelectMany(x => x)),
                parameterExpressions);
        }

        public static Expression<Func<TOuter, TProperty>> ConcactSelector<TInner, TOuter, TProperty>(Expression<Func<TInner, TProperty>> innerSelector,
            string outerName)
        {
            ParameterExpression newParam = Expression.Parameter(typeof(TOuter), "x");
            MemberExpression newParamMember = Expression.Property(newParam, outerName);

            var oldParam = innerSelector.Parameters[0];

            var newSelectorBodyExpression = new ParameterMemberReplaceVisitor(oldParam, newParamMember).Visit(innerSelector.Body);

            var newSelectorExpression = Expression.Lambda<Func<TOuter, TProperty>>(newSelectorBodyExpression, newParam);

            return newSelectorExpression;
        }

        public static Expression<Func<TSource, TDestination>> ConcactAndCombineJoinSelector<TSource, TDestination>(params (Type NavigationType, string NavigationName, IEnumerable<LambdaExpression> Selectors)[] navigationSelectors)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var binderResults = ConcactAndCombineJoinSelectorCore(sourceType, destinationType, navigationSelectors);

            var result = Expression.Lambda<Func<TSource, TDestination>>(
                Expression.MemberInit(
                    Expression.New(destinationType.GetConstructor(Type.EmptyTypes)),
                    binderResults.MemberBindings),
                binderResults.ParameterExpression);

            return result;
        }

        public static Expression<Func<TSource, TDestination>> ConcactAndCombineJoinSelector<TSource, TDestination>(params (Type NavigationType, string NavigationName, LambdaExpression Selector)[] navigationSelectors)
        {
            var convertedSelectors = navigationSelectors
                .Select(x => (NavigationType: x.NavigationType, NavigationName: x.NavigationName, Selectors: new LambdaExpression[] { x.Selector }.AsEnumerable()))
                .ToArray();

            return ConcactAndCombineJoinSelector<TSource, TDestination>(convertedSelectors);
        }

        public static LambdaExpression ConcactAndCombineJoinSelector(Type sourceType,
            Type destinationType,
            params (Type NavigationType, string NavigationName, IEnumerable<LambdaExpression> Selectors)[] navigationSelectors)
        {
            var binderResults = ConcactAndCombineJoinSelectorCore(sourceType, destinationType, navigationSelectors);

            var result = Expression.Lambda(
                Expression.MemberInit(
                    Expression.New(destinationType.GetConstructor(Type.EmptyTypes)),
                    binderResults.MemberBindings),
                binderResults.ParameterExpression);

            return result;
        }

        public static LambdaExpression ConcactAndCombineJoinSelector(Type sourceType,
            Type destinationType,
            params (Type NavigationType, string NavigationName, LambdaExpression Selector)[] navigationSelectors)
        {
            var convertedSelectors = navigationSelectors
                .Select(x => (NavigationType: x.NavigationType, NavigationName: x.NavigationName, Selectors: new LambdaExpression[] { x.Selector }.AsEnumerable()))
                .ToArray();

            return ConcactAndCombineJoinSelector(sourceType, destinationType, convertedSelectors);
        }

        private static ConcactAndCombineJoinSelectorResultInternal ConcactAndCombineJoinSelectorCore(Type sourceType,
            Type destinationType,
            params (Type NavigationType, string NavigationName, IEnumerable<LambdaExpression> Selectors)[] navigation)
        {
            if (navigation == null || !navigation.Any())
            {
                //How to return empty selector?
                throw new NotImplementedException();
            }

            var typesCount = navigation.Length;

            ParameterExpression paramNewResultSeletor = Expression.Parameter(sourceType, "x");

            var binders = new IEnumerable<MemberAssignment>[typesCount];

            for (int i = 0; i < typesCount; i++)
            {
                var paramName = $"x{i + 1}";

                var param = Expression.Parameter(navigation[i].NavigationType, paramName);

                MemberExpression newMember = Expression.Property(paramNewResultSeletor, navigation[i].NavigationName);

                List<MemberAssignment> bindersForType = new List<MemberAssignment>();

                foreach (var selector in navigation[i].Selectors ?? new List<LambdaExpression>())
                {
                    var parameterReplace = new ParameterReplaceVisitor(selector.Parameters[0], param);

                    var memberInitExpression = selector.Body as MemberInitExpression;

                    if (memberInitExpression == null)
                    {
                        //we should only combine when it's MemberInitExpression
                        //for MemberExpression since no SelectorBindingModel, we don't know bind to which property
                        //we maybe can use var targetMemberInfo = destinationType.GetMember(sourceMemberInfo.Name).FirstOrDefault(), but not good
                        //So force use SelectorBindingModel if it's MemberExpression
                        throw new NotImplementedException("Expression type wrong", new ArgumentException(string.Format(
                             "Expression '{0}' must be MemberInitExpression.",
                             selector.ToString())));
                    }

                    if (memberInitExpression.Type != destinationType)
                    {
                        //we should not bind in this case because target Type and source Type are not same, it may have problems, we need SelectorBindingModel
                        throw new NotImplementedException("Expression type wrong", new ArgumentException(string.Format(
                            "Expression '{0}' must be MemberInitExpression of destination type.",
                            selector.ToString())));
                    }

                    foreach (MemberAssignment sourceBinding in memberInitExpression.Bindings)
                    {
                        //var targetMemberInfo = destinationType.GetMember(sourceBinding.Member.Name).FirstOrDefault();
                        var targetMemberInfo = sourceBinding.Member;

                        var newSelector = Expression.Bind(targetMemberInfo, parameterReplace.VisitAndConvert(sourceBinding.Expression, "Combine"));

                        var newSelectorExpression = new ParameterMemberReplaceVisitor(param, newMember).Visit(newSelector.Expression);

                        //replace member, ex. x.P1 => x.Navigation.P1
                        newSelector = newSelector.Update(newSelectorExpression);

                        bindersForType.Add(newSelector);
                    }

                }

                binders[i] = bindersForType;
            }

            var result = new ConcactAndCombineJoinSelectorResultInternal
            {
                ParameterExpression = paramNewResultSeletor,
                MemberBindings = binders.SelectMany(x => x),
            };

            return result;
        }

        public static Expression<Func<TSource, TDestination>> ConcactAndCombineJoinSelector<TSource, TDestination>(params (Type NavigationType, string NavigationName, IEnumerable<SelectorBindingModel> Selectors)[] navigationSelectors)
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var binderResults = ConcactAndCombineJoinSelectorCore(sourceType, destinationType, navigationSelectors);

            var result = Expression.Lambda<Func<TSource, TDestination>>(
                Expression.MemberInit(
                    Expression.New(destinationType.GetConstructor(Type.EmptyTypes)),
                    binderResults.MemberBindings),
                binderResults.ParameterExpression);

            return result;
        }
        
        public static LambdaExpression ConcactAndCombineJoinSelector(Type sourceType,
            Type destinationType,
            params (Type NavigationType, string NavigationName, IEnumerable<SelectorBindingModel> Selectors)[] navigationSelectors)
        {
            var binderResults = ConcactAndCombineJoinSelectorCore(sourceType, destinationType, navigationSelectors);

            var result = Expression.Lambda(
                Expression.MemberInit(
                    Expression.New(destinationType.GetConstructor(Type.EmptyTypes)),
                    binderResults.MemberBindings),
                binderResults.ParameterExpression);

            return result;
        }

        private static ConcactAndCombineJoinSelectorResultInternal ConcactAndCombineJoinSelectorCore(Type sourceType,
            Type destinationType,
            params (Type NavigationType, string NavigationName, IEnumerable<SelectorBindingModel> Selectors)[] navigation)
        {
            if (navigation == null || !navigation.Any())
            {
                //How to return empty selector?
                throw new NotImplementedException();
            }

            var typesCount = navigation.Length;

            ParameterExpression paramNewResultSeletor = Expression.Parameter(sourceType, "x");

            var binders = new IEnumerable<MemberAssignment>[typesCount];

            for (int i = 0; i < typesCount; i++)
            {
                var paramName = $"x{i + 1}";

                var param = Expression.Parameter(navigation[i].NavigationType, paramName);

                MemberExpression newMember = Expression.Property(paramNewResultSeletor, navigation[i].NavigationName);

                List<MemberAssignment> bindersForType = new List<MemberAssignment>();

                foreach (var selector in navigation[i].Selectors ?? new List<SelectorBindingModel>())
                {
                    var parameterReplace = new ParameterReplaceVisitor(selector.SourceSelector.Parameters[0], param);
                    var targetMemberInfo = destinationType.GetMember(selector.TargetBindingPropertyName).FirstOrDefault();

                    //replace parameter
                    var newSelector = Expression.Bind(targetMemberInfo, parameterReplace.VisitAndConvert(selector.SourceSelector.Body, "Combine"));

                    var newSelectorExpression = new ParameterMemberReplaceVisitor(param, newMember).Visit(newSelector.Expression);

                    //replace member, ex. x.P1 => x.Navigation.P1
                    newSelector = newSelector.Update(newSelectorExpression);

                    bindersForType.Add(newSelector);
                }

                binders[i] = bindersForType;
            }

            var result = new ConcactAndCombineJoinSelectorResultInternal
            {
                ParameterExpression = paramNewResultSeletor,
                MemberBindings = binders.SelectMany(x => x),
            };

            return result;
        }

        class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression from, to;

            public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
            {
                this.from = from;
                this.to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == from ? to : base.VisitParameter(node);
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

        class ParameterMemberReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly Expression _to;

            public ParameterMemberReplaceVisitor(ParameterExpression from, Expression to)
            {
                this._from = from;
                this._to = to;
            }

            public override Expression Visit(Expression exp)
            {
                if (exp == _from)
                {
                    return _to;
                }

                return base.Visit(exp);
            }
        }

        class ConcactAndCombineJoinSelectorResultInternal
        {
            public ParameterExpression ParameterExpression { get; set; }
            public IEnumerable<MemberBinding> MemberBindings { get; set; }
        }
    }

    public class SelectorBindingModel
    {
        public LambdaExpression SourceSelector { get; set; }
        public string TargetBindingPropertyName { get; set; }
    }
}
