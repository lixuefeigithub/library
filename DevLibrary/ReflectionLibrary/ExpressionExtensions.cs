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
    }
}
