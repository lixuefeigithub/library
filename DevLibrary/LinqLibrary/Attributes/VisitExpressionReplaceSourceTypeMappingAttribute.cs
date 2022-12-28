using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqLibrary.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class VisitExpressionReplaceSourceTypeMappingAttribute : Attribute
    {
        public Type TargetType { get; set; }
        public string TargetTypePropertyName { get; set; }

        public bool IsMatchTargetType(Type realTargetType)
        {
            if (TargetType == null)
            {
                return false;
            }

            if (realTargetType == null)
            {
                return false;
            }

            if (TargetType.IsAssignableFrom(realTargetType))
            {
                return true;
            }

            if (realTargetType.IsGenericType && TargetType.IsAssignableFrom(realTargetType.GetGenericTypeDefinition()))
            {
                return true;
            }

            if (TargetType.IsInterface)
            {
                var entityTypeInterfaces = realTargetType.GetInterfaces();

                if (entityTypeInterfaces.Any(i => TargetType.IsAssignableFrom(i)))
                {
                    return true;
                }

                if (entityTypeInterfaces.Any(i => i.IsGenericType && TargetType.IsAssignableFrom(i.GetGenericTypeDefinition())))
                {
                    return true;
                }
            }

            if (!TargetType.IsInterface && TargetType.IsGenericType)
            {
                var baseType = realTargetType.BaseType;

                while (baseType != null && baseType != typeof(object))
                {
                    if (baseType.IsGenericType && TargetType.IsAssignableFrom(baseType.GetGenericTypeDefinition()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
