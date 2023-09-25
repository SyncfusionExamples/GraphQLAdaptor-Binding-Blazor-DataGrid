using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace ASPNetCoreGraphQlServer.Data
{
    public static class EnumerableExtensions
    {

        public static ParameterExpression Parameter(this Type sourceType)
        {
            return Expression.Parameter(sourceType, sourceType?.Name);
        }

        public static Type GetElementType(this IEnumerable source)
        {
            return GetElementTypeByRepresentativeItem(source, false);
        }

        internal static Type GetElementTypeByRepresentativeItem(this IEnumerable source, bool useRepresentativeItem)
        {
            var list = source;

            // var prop = list.GetType().GetProperty("Item");
            var prop = list.GetItemPropertyInfo();
            return prop != null ? prop.PropertyType : GetItemType(source, useRepresentativeItem);
        }

        public static Type GetItemType(this IEnumerable source, bool useRepresentativeItem)
        {
            var type = source?.GetType();
#if EJ2_DNX
            if (type.IsGenericType)
#else
            if (type.GetTypeInfo().IsGenericType)
#endif
            {
                var generictype = GetBaseGenericInterfaceType(type, true);
#if EJ2_DNX
                if (generictype == null || generictype.IsInterface || generictype.IsAbstract)
#else
                if (generictype == null || generictype.GetTypeInfo().IsInterface || generictype.GetTypeInfo().IsAbstract)
#endif
                {
                    if (useRepresentativeItem)
                    {
                        var representativeItem = GetRepresentativeItem(source);
                        if (representativeItem != null)
                        {
                            return representativeItem.GetType();
                        }
                    }
                }

                return generictype;
            }
            else if (useRepresentativeItem)
            {
                var representativeItem = GetRepresentativeItem(source);
                if (representativeItem != null)
                {
                    return representativeItem.GetType();
                }
#if !EJ2_DNX
                else if (type.GetTypeInfo().BaseType != null && type.GetTypeInfo().BaseType.GetTypeInfo().IsGenericType)
                {
                    return type.GetTypeInfo().BaseType.GetGenericArguments()[0];
                }
#else
                else if (type.BaseType != null && type.BaseType.IsGenericType)
                    return type.BaseType.GetGenericArguments()[0];
#endif
            }

            return null;
        }

        private static object GetRepresentativeItem(IEnumerable source)
        {
            var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext())
            {
                return enumerator.Current;
            }

            return null;
        }

        private static Type GetBaseGenericInterfaceType(Type type, bool canreturn)
        {
#if EJ2_DNX
            if (type.IsGenericType)
#else
            if (type.GetTypeInfo().IsGenericType)
#endif
            {
                Type[] genericArguments = type.GetGenericArguments();
                if (genericArguments.Length == 1)
                {
#if EJ2_DNX
                    if (genericArguments[0].IsInterface || genericArguments[0].IsAbstract)
#else
                    if (genericArguments[0].GetTypeInfo().IsInterface || genericArguments[0].GetTypeInfo().IsAbstract)
#endif
                    {
                        return genericArguments[0];
                    }

                    if (canreturn)
                    {
                        return genericArguments[0];
                    }
                }
            }
#if EJ2_DNX
            else if (type.BaseType != null)
#else
            else if (type.GetTypeInfo().BaseType != null)
#endif
            {
#if EJ2_DNX
                return GetBaseGenericInterfaceType(type.BaseType, canreturn);
#else
                return GetBaseGenericInterfaceType(type.GetTypeInfo().BaseType, canreturn);
#endif
            }

            return null;
        }

        public static PropertyInfo GetItemPropertyInfo(this IEnumerable list)
        {
            var prop = list?.GetType().GetProperties().Where(p => p.Name.Equals("Item", StringComparison.Ordinal));
            if (prop.Count() > 1)
            {
                return prop.FirstOrDefault(p =>
                {
                    ParameterInfo[] para = p.GetGetMethod().GetParameters();
                    if (para.Any())
                    {
                        return para[0].ParameterType == typeof(int);
                    }

                    return false;
                });
            }
            else
            {
                return list.GetType().GetProperty("Item");
            }
        }


    }
}
