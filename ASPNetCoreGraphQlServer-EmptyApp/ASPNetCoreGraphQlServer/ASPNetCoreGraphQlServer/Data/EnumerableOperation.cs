using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;
using System.Dynamic;
using ASPNetCoreGraphQlServer.Models;

namespace ASPNetCoreGraphQlServer.Data
{
    /// <summary>
    ///
    /// </summary>
    public static class EnumerableOperation
    {
        /// <summary>
        /// Executes the query against the given data source and returns the resultant records.
        /// </summary>
        /// <param name="dataSource">Input data source against which the query to be executed.</param>
        /// <param name="manager">Query to be executed.</param>
        /// <returns>IEnumerable - resultant records.</returns>
        public static IEnumerable Execute(IEnumerable dataSource, DataManagerRequest manager)
        {
            if (manager == null) { return dataSource; }
            if (manager.Where != null && manager.Where.Count > 0)
            {
                dataSource = PerformFiltering(dataSource, manager.Where, manager.Where[0].Operator);
            }

            if (manager.Search != null && manager.Search.Count > 0)
            {
                dataSource = PerformSearching(dataSource, manager.Search);
            }

            if (manager.Sorted != null && manager.Sorted.Count > 0)
            {
                dataSource = PerformSorting(dataSource, manager.Sorted);
            }

            // if (manager.Select != null && manager.Select.Count > 0)
            //    dataSource = PerformSelect(dataSource, manager.Select);
            if (manager.Skip != 0)
            {
                dataSource = PerformSkip(dataSource, manager.Skip);
            }

            if (manager.Take != 0)
            {
                dataSource = PerformTake(dataSource, manager.Take);
            }

            return dataSource;
        }

        /// <summary>
        /// Groups data source by the given list of column names.
        /// </summary>
        /// <param name="dataSource">Input data source to be grouped.</param>
        /// <param name="grouped">List of column names by which rows will be grouped.</param>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable<GroupResult> PerformGrouping(IEnumerable dataSource, List<string> grouped)
        {
            if (dataSource == null || grouped == null)
                throw new ArgumentNullException(nameof(dataSource),"Data source or group can't be null in PerformGrouping");

            IQueryable dataSourceQuery = dataSource.AsQueryable();

            Func<string, Expression> getvalufunc = null;
            Type objType = dataSource.GetElementType();
            var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(objType);
            if (isdyanmic)
            {
                Expression<Func<string, object, object>> valufunc = (propertyName, obj) => ReflectionExtension.GetValue(obj, propertyName, true);
                getvalufunc = propertyName => valufunc;
            }

            if (getvalufunc == null)
                return dataSourceQuery.GroupByMany(grouped).AsQueryable();
            else
                return dataSource.GroupByMany(objType, getvalufunc, grouped.ToArray()).AsQueryable();
        }

        /// <summary>
        /// Sorts the data source using the given sort descriptor and returns the sorted records.
        /// </summary>
        /// <param name="dataSource">Data source to be sorted.</param>
        /// <param name="sortedColumns">List of sort criteria.</param>
        /// <param name="sourceType">Specifies the source type.</param>
        /// <returns>IEnumerable - sorted records.</returns>
        public static IEnumerable PerformSorting(IEnumerable dataSource, List<SortedColumn> sortedColumns, Type sourceType = null)
        {
            IQueryable data = dataSource.AsQueryable();
            
            sourceType ??= data.GetObjectType();
#pragma warning disable CA1508
            Expression<Func<string, object, object>> valuexpressionfunc = null;
            var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(sourceType);
            if (isdyanmic)
                valuexpressionfunc = (propertyName, obj) => ReflectionExtension.GetValue(obj, propertyName, true);

            bool firstTime = true;
            foreach (var column in sortedColumns ?? new List<SortedColumn>())
            {
                if (column.Direction == SortOrder.Ascending)
                {
                    if (firstTime)
                    {
                        if (valuexpressionfunc == null)
                        {
                            if (column.Comparer != null)
                            {
                                data = data.OrderBy(column.Field, column.Comparer as IComparer<object>, sourceType);
                            }
                            else
                            {
                                data = data.OrderBy(column.Field, sourceType);
                            }
                        }
                        else
                        {
                            if (column.Comparer != null)
                            {
                                data = data.OrderBy(column.Field, column.Comparer as IComparer<object>, valuexpressionfunc);
                            }
                            else
                            {
                                data = data.OrderBy(column.Field, valuexpressionfunc);
                            }

                        }
                        firstTime = false;
                    }
                    else
                    {
                        if (valuexpressionfunc == null)
                        {
                            if (column.Comparer != null)
                            {
                                data = data.ThenBy(column.Field, column.Comparer as IComparer<object>, sourceType);
                            }
                            else
                            {
                                data = data.ThenBy(column.Field, sourceType);
                            }
                        }
                        else
                        {
                            if (column.Comparer != null)
                            {
                                data = data.ThenBy(column.Field, column.Comparer as IComparer<object>, valuexpressionfunc);
                            }
                            else
                            {
                                data = data.ThenBy(column.Field, valuexpressionfunc);
                            }
                        }
                    }
                }
                else
                {
                    if (firstTime)
                    {
                        if (valuexpressionfunc == null)
                        {
                            if (column.Comparer != null)
                            {
                                data = data.OrderByDescending(column.Field, column.Comparer as IComparer<object>, sourceType);
                            }
                            else
                            {
                                data = data.OrderByDescending(column.Field, sourceType);
                            }
                        }
                        else
                        {
                            if (column.Comparer != null)
                            {
                                data = data.OrderByDescending(column.Field, column.Comparer as IComparer<object>, valuexpressionfunc);
                            }
                            else
                            {
                                data = data.OrderByDescending(column.Field, valuexpressionfunc);
                            }
                        }
                        firstTime = false;
                    }
                    else
                    {
                        if (valuexpressionfunc == null)
                        {
                            if (column.Comparer != null)
                            {
                                data = data.ThenByDescending(column.Field, column.Comparer as IComparer<object>, sourceType);
                            }
                            else
                            {
                                data = data.ThenByDescending(column.Field, sourceType);
                            }
                        }
                        else
                        {
                            if (column.Comparer != null)
                            {
                                data = data.ThenByDescending(column.Field, column.Comparer as IComparer<object>, valuexpressionfunc);
                            }
                            else
                            {
                                data = data.ThenByDescending(column.Field, valuexpressionfunc);
                            }
                        }
                    }
                }
            }
#pragma warning restore CA1508
            return data;
        }

        /// <summary>
        /// Sorts the data source using the given sort descriptor and returns the sorted records.
        /// </summary>
        /// <param name="dataSource">Data source to be sorted.</param>
        /// <param name="sortedColumns">List of sort criteria.</param>
        /// <returns>IEnumerable - sorted records.</returns>
        public static IEnumerable PerformSorting(IEnumerable dataSource, List<Sort> sortedColumns)
        {
            IEnumerable data = (IEnumerable)dataSource;
            if (dataSource != null && !dataSource.GetEnumerator().MoveNext())
            {
                return dataSource;
            }
            List<SortedColumn> sortedColumn = new List<SortedColumn>();
            if (sortedColumns != null && sortedColumns.Count > 1)
            {
                sortedColumns.Reverse();
            }

            foreach (var column in sortedColumns ?? new List<Sort>())
            {
                var direction = (SortOrder)Enum.Parse(typeof(SortOrder), column.Direction.ToString(), true);
                sortedColumn.Add(new SortedColumn { Direction = direction, Field = column.Name, Comparer = column.Comparer });
            }

            data = PerformSorting(data, sortedColumn);
            return data;
        }

        /// <summary>
        /// Generates predicate with the given filter criteria.
        /// </summary>
        /// <param name="dataSource">Input data source.</param>
        /// <param name="whereFilter">List of filter criteria.</param>
        /// <param name="condition">Value can be either AND or OR.</param>
        /// <param name="paramExpression">Parameter expression.</param>
        /// <param name="type">Specifies the source type.</param>
        /// <returns>Expression.</returns>
        public static Expression PredicateBuilder(IEnumerable dataSource, List<WhereFilter> whereFilter, string condition, ParameterExpression paramExpression, Type type)
        {
            Type t = typeof(object);
            Expression predicate = null;
            foreach (var filter in whereFilter ?? new List<WhereFilter>())
            {
                if ((bool)filter.IsComplex)
                {
                    if (predicate == null)
                    {
                        predicate = PredicateBuilder(dataSource, filter.predicates, filter.Condition, paramExpression, type);
                    }
                    else
                    {
                        if (condition == "or")
                        {
                            predicate = predicate.OrElsePredicate(PredicateBuilder(dataSource, filter.predicates, filter.Condition, paramExpression, type));
                        }
                        else
                        {
                            predicate = predicate.AndAlsoPredicate(PredicateBuilder(dataSource, filter.predicates, filter.Condition, paramExpression, type));
                        }
                    }
                }
                else
                {
                    var op = filter.Operator;
                    if (op == "equal")
                    {
                        op = "equals";
                    }
                    else if (op == "notequal")
                    {
                        op = "notequals";
                    }

                    FilterType filterType = (FilterType)Enum.Parse(typeof(FilterType), op.ToString(), true);
                    type = GetDataType(dataSource, type, filter.Field);
                    t = GetColumnType(dataSource, filter.Field, type);
                    if (t == null) return null;
                    var enumValue = new object();
                    Type underlyingType = Nullable.GetUnderlyingType(t);
                    if (underlyingType != null)
                    {
                        t = underlyingType;
                    }
                    if (t.IsEnum)
                    {
                        Type EnumPropType = DataUtil.GetEnumType(filter.Field, type);
                        enumValue = filter.value != null ?  EnumerationValue.GetValueFromEnumMember(filter.value.ToString(), EnumPropType) : null;
                        if (enumValue == null) // if enumvalue and enummember value are different then use enum value.
                        {
                            Enum.TryParse(EnumPropType, filter.value?.ToString(), out enumValue);
                        }
                    }

                    var value = filter.value;
                    if (value != null)
                    {
                        if (t == typeof(Guid))
                        {
                            value = (Guid)TypeDescriptor.GetConverter(typeof(Guid)).ConvertFromInvariantString(filter.value.ToString());
                        }
                        else if (filter.value.GetType().Name == t.Name || filter.value.GetType().Name == "JsonElement")
                        {
                            value = t.IsEnum ? enumValue : NullableHelperInternal.ChangeType(filter.value, t);
                        }
                        
                    }

                    if (predicate == null)
                    {
                        predicate = dataSource.AsQueryable().Predicate(paramExpression, filter.Field, value, filterType, FilterBehavior.StringTyped, (bool)!filter.IgnoreCase, type);
                    }
                    else
                    {
                        if (condition == "or")
                        {
                            predicate = predicate.OrPredicate(dataSource.AsQueryable().Predicate(paramExpression, filter.Field, value, filterType, FilterBehavior.StringTyped, (bool)!filter.IgnoreCase, type));
                        }
                        else
                        {
                            predicate = predicate.AndPredicate(dataSource.AsQueryable().Predicate(paramExpression, filter.Field, value, filterType, FilterBehavior.StringTyped, (bool)!filter.IgnoreCase, type));
                        }
                    }
                }
            }

            return predicate;
        }

        /// <summary>
        /// Apply the given filter criteria against the data source and returns the filtered records.
        /// </summary>
        /// <param name="dataSource">Data source to be filtered.</param>
        /// <param name="whereFilter">List of filter criteria.</param>
        /// <param name="condition">Filter merge condition. Value can be either AND or OR.</param>
        /// <returns>IEnumerable - filtered records.</returns>
        public static IEnumerable PerformFiltering(IEnumerable dataSource, List<WhereFilter> whereFilter, string condition)
        {
            Type type = dataSource.GetElementType();
            if (type == null)
            {
                Type type1 = dataSource?.GetType();
                type = type1?.GetElementType();
            }

            //var paramExpression = type.Parameter();
            var paramExpression = QueryableExtensions.Parameter(type);
            dataSource = dataSource.AsQueryable().Where(paramExpression, PredicateBuilder(dataSource, whereFilter, condition, paramExpression, type));
            return dataSource;
        }

        // public IEnumerable PerformSelect(IEnumerable dataSource, List<string> select)
        // {
        //    IEnumerable<string> sel = select.Where(item => item != null);
        //    Type type = dataSource.AsQueryable().GetObjectType();
        //    dataSource = dataSource.AsQueryable().Select(sel, type);
        //    return dataSource;
        // }

        /// <summary>
        /// Apply the given search criteria against the data source and returns the filtered records.
        /// </summary>
        /// <param name="dataSource">Data source to be filtered.</param>
        /// <param name="searchFilter">List of search criteria.</param>
        /// <returns>IEnumerable - searched records.</returns>
        public static IEnumerable PerformSearching(IEnumerable dataSource, List<SearchFilter> searchFilter)
        {
            Type type = dataSource.GetElementType();
            Type t = typeof(object);
            if (type == null)
            {
                Type type1 = dataSource?.GetType();
                type = type1?.GetElementType();
            }

            foreach (var filter in searchFilter ?? new List<SearchFilter>())
            {
                //var paramExpression = type.Parameter();
                var paramExpression = QueryableExtensions.Parameter(type);
                var initialLoop = true;
                Expression predicate = null;
                var op = filter.Operator;
                if (op == "equal")
                {
                    op = "equals";
                }
                else if (op == "notequal")
                {
                    op = "notequals";
                }

                FilterType FilterType = (FilterType)Enum.Parse(typeof(FilterType), op.ToString(), true);
                foreach (string fields in filter.Fields)
                {
                    type = GetDataType(dataSource, type, fields);
                    t = GetColumnType(dataSource, fields, type);
                    if (t == null) continue;
                    var enumValue = new object();
                    if (t.IsEnum)
                    {
                        Type EnumPropType = DataUtil.GetEnumType(fields, type);
                        enumValue = EnumerationValue.GetValueFromEnumMember(filter.Key.ToString(), EnumPropType);
                        if (enumValue == null) // if enumvalue and enummember value are different then use enum value.
                        {
                            Enum.TryParse(EnumPropType, filter.Key.ToString(), out enumValue);
                        }
                    }

                    if (initialLoop && !t.IsEnum)
                    {
                        predicate = dataSource.AsQueryable().Predicate(paramExpression, fields, t.IsEnum ? enumValue : filter.Key, FilterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type);
                        initialLoop = false;
                    }
                    else if ((t.IsEnum && NullableHelperInternal.IsNullableType(t)) || (t.IsEnum && enumValue != null))
                    {
                        if (!initialLoop)
                        {
                            predicate = predicate.OrPredicate(dataSource.AsQueryable().Predicate(paramExpression, fields, t.IsEnum ? enumValue : filter.Key, FilterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type));
                        }
                        else
                        {
                            predicate = dataSource.AsQueryable().Predicate(paramExpression, fields, t.IsEnum ? enumValue : filter.Key, FilterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type);
                        }

                        initialLoop = false;
                    }
                    else if (!t.IsEnum)
                    {
                        predicate = predicate.OrPredicate(dataSource.AsQueryable().Predicate(paramExpression, fields, t.IsEnum ? enumValue : filter.Key, FilterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type));
                    }
                }

                dataSource = dataSource.AsQueryable().Where(paramExpression, predicate);
            }

            return dataSource;
        }

        /// <summary>
        /// Returns data type.
        /// </summary>
        /// <exclude/>
        public static Type GetDataType(IEnumerable dataSource, Type type, string field)
        {
            string[] complexData = field != null ? field.Split('.') : Array.Empty<string>();
            if (type != null && type.GetProperty(complexData[0]) == null)
            {
                type = dataSource.AsQueryable().GetObjectType();
            }

            return type;
        }

        /// <summary>
        /// Returns column type.
        /// </summary>
        /// <exclude/>
        public static Type GetColumnType(IEnumerable dataSource, string filterString, Type type)
        {
            string[] complexData = filterString != null ? filterString.Split('.') : Array.Empty<string>(); ;
            PropertyInfo propInfo = null;
            for (var i = 0; i < complexData.Length; i++)
            {
                int n;
                if (int.TryParse(complexData[i], out n))
                {
                    type = type?.GetProperties()[2].PropertyType;
                }
                else if (string.Equals(type?.Name, "ExpandoObject", StringComparison.Ordinal))
                {
                    var value = DataUtil.GetObject(filterString, dataSource.AsQueryable().ElementAt(0));
                    if (value == null && dataSource != null)
                    {
                        type = updateType(dataSource, filterString, value, type);
                    }
                    else
                    {
                        type = value.GetType();
                    }
                    return type;
                }
                else if (type.IsSubclassOf(typeof(DynamicObject)))
                {
                    var value = DataUtil.GetObject(filterString, dataSource.AsQueryable().ElementAt(0));
                    if (value == null && dataSource != null)
                    {
                        type = updateType(dataSource, filterString, value, type);
                    }
                    else
                    {
                        type = value.GetType();
                    }
                    return type;
                }
                else
                {
                    propInfo = type.GetProperty(complexData[i], BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    type = propInfo?.PropertyType;
                }
            }

            return propInfo?.PropertyType;
        }

        private static Type updateType(IEnumerable dataSource, string filterString, object value, Type type)
        {
            bool isValue = false;
            foreach (var item in dataSource)
            {
                value = DataUtil.GetObject(filterString, item);
                if (value != null)
                {
                    isValue = true;
                    break;
                }
            }
            if (isValue)
            {
                type = value.GetType();
            }
            return type;
        }

        /// <summary>
        /// Skip the given number of records from data source and returns the resultant records.
        /// </summary>
        /// <param name="dataSource">Input data source.</param>
        /// <param name="skip">Number of records to be skipped.</param>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable PerformSkip(IEnumerable dataSource, int skip)
        {
            IEnumerable data = (IEnumerable)dataSource;
            return data.AsQueryable().Skip(skip);
        }

        /// <summary>
        /// Take the given number of records from data source.
        /// </summary>
        /// <param name="dataSource">Input data source.</param>
        /// <param name="take">Number of records to be taken.</param>
        /// <returns>IEnumerable.</returns>
        public static IEnumerable PerformTake(IEnumerable dataSource, int take)
        {
            IEnumerable data = (IEnumerable)dataSource;
            return data.AsQueryable().Take(take);
        }
    }
}
