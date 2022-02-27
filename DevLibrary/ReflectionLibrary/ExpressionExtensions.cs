using System;
using System.Linq.Expressions;
using System.Reflection;

namespace ReflectionLibrary
{
    public static class ExpressionExtensions
    {
        public static PropertyInfo GetPropertyInfo(this LambdaExpression propertyLambdaExpression)
        {
            var memberSelectorExpression = propertyLambdaExpression.Body as MemberExpression;

            if (memberSelectorExpression != null)
            {
                var property = memberSelectorExpression.Member as PropertyInfo;

                return property;
            }

            return null;
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
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
    }
}
