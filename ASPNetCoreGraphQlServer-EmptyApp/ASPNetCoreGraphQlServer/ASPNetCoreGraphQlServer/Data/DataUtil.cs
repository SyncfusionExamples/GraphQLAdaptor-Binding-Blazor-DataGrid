using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Globalization;
using System.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Text.Json;
using ASPNetCoreGraphQlServer.Models;

namespace ASPNetCoreGraphQlServer.Data
{
    /// <summary>
    /// Provides utility method used by data manager.
    /// </summary>
    public static class DataUtil
    {
        /// <summary>
        /// Resolves the given base url and relative url to generate absolute url. And merge query string if any.
        /// </summary>
        /// <param name="baseUrl">Base address url.</param>
        /// <param name="relativeUrl">Relative url.</param>
        /// <param name="queryParams">Query string.</param>
        /// <returns>string - absolute url.</returns>
        public static string GetUrl(string baseUrl, string relativeUrl, string queryParams = null)
        {
            var bHasSlash = !string.IsNullOrEmpty(baseUrl) && baseUrl[baseUrl.Length - 1] == '/';
            string url = baseUrl;
            var queryString = string.Empty;

            if (!string.IsNullOrEmpty(relativeUrl))
            {
                var rHasSlash = !string.IsNullOrEmpty(relativeUrl) && relativeUrl[0] == '/';
                if (bHasSlash ^ rHasSlash)
                {
                    url = $"{baseUrl}{relativeUrl}";
                }
                else if (!bHasSlash && !rHasSlash)
                {
                    url = $"{baseUrl}/{relativeUrl}";
                }
                else if (bHasSlash && rHasSlash)
                {
                    url = $"{baseUrl}{relativeUrl.Substring(1, relativeUrl.Length - 1)}";
                }
                else
                {
                    url = $"{baseUrl}{relativeUrl}";
                }
            }

            if (string.IsNullOrEmpty(queryParams))
            {
                return url;
            }

            // Query parameters process
            if (url[url.Length - 1] != '?' && url.IndexOf("?", StringComparison.Ordinal) > -1)
            {
                queryString = $"&{queryParams}";
            }
            else if (url.IndexOf("?", StringComparison.Ordinal) < 0 && !string.IsNullOrEmpty(queryParams))
            {
                queryString = $"?{queryParams}";
            }

            return url + queryString;
        }

        /// <summary>
        /// Gets the property value with the given key.
        /// </summary>
        /// <param name="key">Property name.</param>
        /// <param name="value">Source object.</param>
        /// <returns>string.</returns>
        public static string GetKeyValue(string key, object value)
        {
            PropertyInfo propInfo = value?.GetType().GetProperty(key);
            Type propType = propInfo.PropertyType;

            // Check nullable types.
            if (propType.IsGenericType && propType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                propType = NullableHelperInternal.GetUnderlyingType(propType);
            }

            object propVal = propInfo.GetValue(value);
            
            if (propType.Name == "DateTime")
            {
                propVal = ((DateTime)propVal).ToString("s", CultureInfo.InvariantCulture);
            }

            return propVal?.ToString();
        }

        /// <summary>
        /// Converts dictionary of key/value pair to query string.
        /// </summary>
        /// <param name="Params">Input dictionary value.</param>
        /// <returns>string - Query string.</returns>
        public static string ToQueryParams(IDictionary<string, object> Params)
        {
            string[] sb = new string[Params != null ? Params.Count : 0];
            var i = 0;
            foreach (var param in Params ?? new Dictionary<string, object>())
            {
                if (param.Value != null)
                {
                    sb[i++] = $"{param.Key}={param.Value.ToString()}";
                }
            }

            return string.Join("&", sb);
        }

        /// <summary>
        /// Converts dictionary of key/value pair to query string.
        /// </summary>
        /// <param name="dataSource">Collection of Data source.</param>
        /// <param name="propertyName">property name which is need to distincts </param>.
        /// <returns>IEnumerable Distinct collections</returns>
        internal static IEnumerable<T> GetDistinct<T>(IEnumerable<T> dataSource, string propertyName)
        {
            List<T> DistinctCollections = new List<T>();
            var DistinctData = new Dictionary<string, object>();

            var complexNameSpace = propertyName.Contains('.', StringComparison.InvariantCulture) ? propertyName.Split(".")[0] : propertyName;

            foreach (var value in dataSource)
            {
                if (value is ExpandoObject)
                {
                    var dictionaryValue = value as IDictionary<string, object>;
                    if (dictionaryValue != null && !dictionaryValue.ContainsKey(complexNameSpace))
                    {
                        continue;
                    }
                }

                var propertyValue = GetObject(propertyName, value);
                string key = propertyValue == null ? "null" : propertyValue.ToString();

                if (!DistinctData.ContainsKey(key))
                {
                    DistinctData.Add(key, value);
                    DistinctCollections.Add(value);
                }
            }

            return DistinctCollections.AsEnumerable<T>();
        }

        internal static IDictionary<string, string> odUniOperator = new Dictionary<string, string>()
        {
            { "$=", "endswith" },
            { "^=", "startswith" },
            { "*=", "substringof" },
            { "endswith", "endswith" },
            { "startswith", "startswith" },
            { "contains", "substringof" }
        };

        internal static IDictionary<string, string> odBiOperator = new Dictionary<string, string>()
        {
            { "<", " lt " },
            { ">", " gt " },
            { "<=", " le " },
            { ">=", " ge " },
            { "==", " eq " },
            { "!=", " ne " },
            { "lessthan", " lt " },
            { "lessthanorequal", " le " },
            { "greaterthan", " gt " },
            { "greaterthanorequal", " ge " },
            { "equal", " eq " },
            { "notequal", " ne " }
        };

        internal static IDictionary<string, string> Odv4UniOperator = new Dictionary<string, string>()
        {
            { "$=", "endswith" },
            { "^=", "startswith" },
            { "*=", "contains" },
            { "endswith", "endswith" },
            { "startswith", "startswith" },
            { "contains", "contains" }
        };

        internal static IDictionary<string, string> consts = new Dictionary<string, string>()
            {
                { "GroupGuid", "{271bbba0-1ee7}" }
            };

        /// <summary>
        /// Groups the given data source with the field name.
        /// </summary>
        /// <typeparam name="T">Type of the data source elements.</typeparam>
        /// <param name="jsonArray">Input data source.</param>
        /// <param name="field">Specifies the group by field name.</param>
        /// <param name="aggregates">Aggregate details to aggregate grouped records.</param>
        /// <param name="level">Level of the group. For parent group it is 0.</param>
        /// <param name="format">Specifies the format and handler method to perform group by format.</param>
        /// <param name="isLazyLoad">Specifies the isLazyLoad property as true to handle lazy load grouping.</param>
        /// <param name="isLazyGroupExpandAll">Specifies the isLazyGroupExpandAll as true to perform expand all for lazy load grouping.</param>
        /// <returns>IEnumerable - Grouped record.</returns>
        public static IEnumerable Group<T>(IEnumerable jsonArray, string field, List<Aggregate> aggregates, int level, IDictionary<string, string> format, bool isLazyLoad = false, bool isLazyGroupExpandAll = false)
        {
            if (level == 0)
            {
                level = 1;
            }

            string guid = "GroupGuid";
            if (jsonArray?.GetType().GetProperty(guid) != null && (jsonArray as Group<T>).GroupGuid == consts[guid])
            {
                Group<T> json = (Group<T>)jsonArray;
                for (int j = 0; j < json.Count; j++)
                {
                    json[j].Items = (IEnumerable)Group<T>(json[j].Items, field, aggregates, level + 1,
                        format, isLazyLoad, isLazyGroupExpandAll);
                    json[j].CountItems = json[j].Items.Cast<object>().ToList().Count;
                }

                json.ChildLevels += 1;
                return json;
            }

            object[] jsonData = jsonArray.Cast<object>().ToArray();
            IDictionary<object, Group<T>> grouped = new Dictionary<object, Group<T>>();
            Group<T> groupedArray = new Group<T>() { GroupGuid = consts["GroupGuid"], Level = level, ChildLevels = 0, Records = jsonData };
            for (int i = 0; i < jsonData.Length; i++)
            {
                var val = GetGroupValue(field, jsonData[i]);
                if (format != null && format.TryGetValue(field, out string value) && value != null && val != null)
                {
                    val = DataUtil.GetFormattedValue(val, format[field]);
                }

                if (val == null)
                {
                    val = "null";
                }

                if (!grouped.ContainsKey(val))
                {
                    grouped.Add(val, new Group<T>() { Key = val, CountItems = 0, Level = level, Items = new List<T>(), Aggregates = new object(), Field = field, GroupedData = new List<T>() });
                    groupedArray.Add(grouped[val]);
                }

                grouped[val].CountItems = grouped[val].CountItems += 1;
                if (!isLazyLoad || (isLazyLoad && aggregates != null))
                {
                    (grouped[val].Items as List<T>).Add((T)jsonData[i]);
                }
                if (isLazyLoad || isLazyGroupExpandAll)
                {
                    (grouped[val].GroupedData as List<T>).Add((T)jsonData[i]);
                }
            }

            if (aggregates != null && aggregates.Count > 0)
            {
                for (int i = 0; i < groupedArray.Count; i++)
                {
                    IDictionary<string, object> res = new Dictionary<string, object>();
                    Func<IEnumerable, string, string, object> fn;
                    //var type = groupedArray[i].Items.Cast<object>().ToArray()[0].GetType();
                    groupedArray[i].Items = groupedArray[i].Items as List<T>;
                    for (int j = 0; j < aggregates.Count; j++)
                    {
                        fn = CalculateAggregateFunc();
                        if (fn != null)
                        {
                            res[aggregates[j].Field + " - " + aggregates[j].Type] = fn(groupedArray[i].Items, aggregates[j].Field, aggregates[j].Type);
                        }
                    }

                    groupedArray[i].Aggregates = res;
                }
            }

            return Result<T>(jsonData, isLazyLoad, aggregates, groupedArray);
        }

        public static IEnumerable Result<T>(object[] jsonData, bool isLazyLoad, List<Aggregate> aggregates, Group<T> groupedArray)
        {
            if (jsonData == null) { throw new ArgumentNullException(nameof(jsonData)); }
            if (groupedArray == null) { throw new ArgumentNullException(nameof(groupedArray)); }
            if (jsonData.Length > 0)
            {
                if (isLazyLoad && aggregates != null)
                {
                    for (int i = 0; i < groupedArray.Count; i++)
                    {
                        groupedArray[i].Items = new List<T>();
                    }
                }
                return groupedArray;
            }
            else
            {
                return jsonData;
            }
        }

        /// <summary>
        /// Performs aggregation on the given data source.
        /// </summary>
        /// <param name="jsonData">Input data source.</param>
        /// <param name="aggregates">List of aggregate to be calculated.</param>
        /// <returns>Dictionary of aggregate results.</returns>
        public static IDictionary<string, object> PerformAggregation(IEnumerable jsonData, List<Aggregate> aggregates)
        {
            IDictionary<string, object> res = new Dictionary<string, object>();
            Func<IEnumerable, string, string, object> fn;

            var jsoncol = jsonData?.Cast<object>();
            if (jsonData == null || !jsonData.Cast<object>().Any())
            {
                return res;
            }
            
            var type = jsoncol.FirstOrDefault().GetType();
            IEnumerable ConvData = CastList(type, jsoncol);
            for (int j = 0; j < (aggregates?.Count ?? 0); j++)
            {
                fn = CalculateAggregateFunc();
                if (fn != null)
                {
                    res[aggregates[j].Field + " - " + aggregates[j].Type.ToLowerInvariant()] = fn(ConvData, aggregates[j].Field, aggregates[j].Type);
                }
            }

            return res;
        }

        internal static IEnumerable CastList(Type type, IEnumerable<object> items)
        {
            var enumerableType = typeof(System.Linq.Enumerable);
            var castMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.Cast)).MakeGenericMethod(type);
            var toListMethod = enumerableType.GetMethod(nameof(System.Linq.Enumerable.ToList)).MakeGenericMethod(type);
            var castedItems = castMethod.Invoke(null, new[] { items });
            return toListMethod.Invoke(null, new[] { castedItems }) as IEnumerable;
        }

        /// <summary>
        /// Gets the property value from list of object.
        /// </summary>
        /// <param name="jsonData">List of object.</param>
        /// <param name="index">Index of the item to be processed.</param>
        /// <param name="field">Property name to get value.</param>
        /// <returns>object.</returns>
        public static object GetVal(IEnumerable jsonData, int index, string field)
        {
            var jsonDataCol = jsonData.AsQueryable().Cast<object>();
            if (jsonDataCol.Any())
            {
                if (field != null)
                {
                    return GetObject(field, jsonDataCol.ToArray()[index]);
                }
                else
                {
                    return jsonDataCol.ToArray()[index];
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the property value from object.
        /// </summary>
        /// <param name="nameSpace">Property name to be accessed.</param>
        /// <param name="from">Source object.</param>
        /// <returns>object - property value.</returns>
        public static object GetGroupValue(string nameSpace, Object from)
        {
            if (nameSpace != null)
            {
                return GetObject(nameSpace, from);
            }
            else
            {
                return from;
            }
        }

        /// <summary>
        /// Gets the property value from object.
        /// </summary>
        /// <param name="nameSpace">Property name to be accessed.</param>
        /// <param name="from">Source object.</param>
        /// <returns>object - property value.</returns>
        /// <remarks>For accessing complex/nested property value, given the nameSpace with field names delimited by dot(.).</remarks>
        public static object GetObject(string nameSpace, object from)
        {
            return ReflectionExtension.GetValue(from, nameSpace);
        }

        /// <summary>
        /// Returns enum column type.
        /// </summary>
        /// <exclude/>
        internal static Type GetEnumType(string fieldName, Type type)
        {
            string[] Fields = fieldName.Contains('.', StringComparison.InvariantCulture) ? fieldName.Split(".") : null;
            
            if (Fields != null)
            {
                Type complexType = null;
                for (int v = 0; v < Fields.Length - 1; v++)
                {
                    if (complexType == null)
                    {
                        complexType = type.GetProperty(Fields[v]).PropertyType;
                    }
                    else
                    {
                        complexType = complexType.GetProperty(Fields[v]).PropertyType;
                    }
                }

                return complexType?.GetProperty(Fields[Fields.Length - 1]).PropertyType;
            }
            else
            {
                return type.GetProperty(fieldName).PropertyType;
            }
        }

        internal static Func<IEnumerable, string, string, object> CalculateAggregateFunc()
        {
            return (items, property, pd) =>
            {
                var aggregateType = pd;

                var itemCol = items.Cast<object>();
                var isDynamicObjectType = itemCol.FirstOrDefault().GetType().BaseType == typeof(DynamicObject);
                var isExpandoObjectType = itemCol.FirstOrDefault().GetType() == typeof(ExpandoObject);

                if (isDynamicObjectType || isExpandoObjectType)
                {
                    //IQueryable<IDynamicMetaObjectProvider> dt = items.Cast<IDynamicMetaObjectProvider>().AsQueryable();
                    //switch (aggregateType)
                    //{
                    //    case "Count":
                    //        return dt.Count();
                    //    case "Max":
                    //        return dt.Max(item => ReflectionExtension.GetValueFromIDynamicMetaObject(item, property, true));
                    //    case "Min":
                    //        return dt.Min(item => ReflectionExtension.GetValueFromIDynamicMetaObject(item, property, true));
                    //    case "Average":
                    //        return dt.Select(item => ReflectionExtension.GetValueFromIDynamicMetaObject(item, property, true)).ToList().Average(value => Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    //    case "Sum":
                    //        return dt.Select(item => ReflectionExtension.GetValueFromIDynamicMetaObject(item, property, true)).ToList().Sum(value => Convert.ToDouble(value, CultureInfo.InvariantCulture));
                    //    case "TrueCount":
                    //        List<WhereFilter> trueWhereFilter = new List<WhereFilter>() { new WhereFilter { Field = property, Operator = "equal", value = true } };
                    //        return DynamicObjectOperation.PerformFiltering(items, trueWhereFilter, null, null).Count();
                    //    case "FalseCount":
                    //        List<WhereFilter> falseWhereFilter = new List<WhereFilter>() { new WhereFilter { Field = property, Operator = "equal", value = false } };
                    //        return DynamicObjectOperation.PerformFiltering(items, falseWhereFilter, null, null).Count();
                    //    default:
                           return null;
                    //}
                }
                else
                {
                    IQueryable queryable = items.AsQueryable();
                    switch (aggregateType)
                    {
                        case "Count":
                            return queryable.Count();
                        case "Max":
                            return queryable.Max(property);
                        case "Min":
                            return queryable.Min(property);
                        case "Average":
                            return queryable.Average(property);
                        case "Sum":
                            return queryable.Sum(property);
                        case "TrueCount":
                            return queryable.Where(property, true, FilterType.Equals, false).Count();
                        case "FalseCount":
                            return queryable.Where(property, false, FilterType.Equals, false).Count();
                        default:
                            return null;
                    }
                }
            };
        }

        internal static object CompareAndRemove(object data, object original, string key = "")
        {
            if (original == null)
            {
                return data;
            }

            Type myType = data.GetType();
            var props = new List<PropertyInfo>(myType.GetProperties());

            foreach (PropertyInfo prop in props)
            {
                PropertyInfo orgProp = original.GetType().GetProperty(prop.Name);
                var propertyValue = prop.GetValue(data);
                if (!(propertyValue == null || propertyValue is string || propertyValue.GetType().GetTypeInfo().IsPrimitive || propertyValue is TimeSpan ||
                        propertyValue is decimal || propertyValue is DateTime || propertyValue is TimeOnly || propertyValue is DateOnly || propertyValue is IEnumerable || propertyValue is DateTimeOffset ||
                        propertyValue is ICollection || propertyValue is Guid || propertyValue.GetType().GetTypeInfo().IsEnum))
                {
                    CompareAndRemove(propertyValue, orgProp.GetValue(original));
                    IList<PropertyInfo> propsOfComplex = new List<PropertyInfo>(myType.GetProperties());
                    IEnumerable<PropertyInfo> final = propsOfComplex.Where((data) => data.Name != "@odata.etag");
                    var settings = new JsonSerializerOptions()
                    {
                        WriteIndented = true
                    };
                    settings.Converters.Add(new JsonStringEnumConverter());
                    settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    string serializedData = JsonSerializer.Serialize(propertyValue, settings);
                    if (!final.Any() || serializedData == "{}")
                    {
                        prop.SetValue(data, null);
                    }
                }
                else if (prop.Name != key && prop.Name != "@odata.etag" && propertyValue != null && propertyValue.Equals(orgProp.GetValue(original)))
                {
                    if (propertyValue is bool && prop.PropertyType == typeof(bool))
                    {
                        prop.SetValue(data, propertyValue);
                    }
                    else
                    {
                        prop.SetValue(data, null);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// Formats the given value.
        /// </summary>
        /// <param name="value">Value to be formatted.</param>
        /// <param name="format">Format string.</param>
        /// <returns>string.</returns>
        public static string GetFormattedValue(object value, string format)
        {
            //List<string> Type = new List<string>() { "Double", "Int64", "Int32", "Int16", "Decimal", "Single" };
            //string TypeName = value?.GetType().Name;
            //if (TypeName == "DateTime" || TypeName == "DateTimeOffset" || TypeName == "DateOnly" || TypeName == "TimeOnly")
            //{
            //    return Intl.GetDateFormat<object>(value, format);
            //}
            //else if (value != null && Type.Any(t => TypeName.Contains(t, StringComparison.Ordinal)))
            //{
            //    return Intl.GetNumericFormat<object>((object)value, format);
            //}
            //else
            //{
                return value?.ToString();
            //}
        }

        internal static IDictionary<string, Type> GetColumnType(IEnumerable dataSource, bool nullable = true, string columnName = null)
        {
            _ = columnName;
            IDictionary<string, Type> columnTypes = new Dictionary<string, Type>();
            List<IDynamicMetaObjectProvider> dynamics = dataSource.AsQueryable().Cast<IDynamicMetaObjectProvider>().ToList();
            Type rowType = null;
            if (dynamics.Count > 0)
            {
                rowType = dynamics[0].GetType();
            }
            if (rowType == null || rowType.IsSubclassOf(typeof(DynamicObject)))
            {
                return null;
            }
            var totalRecords = dataSource.AsQueryable().Cast<ExpandoObject>().ToList();
            int count = totalRecords.Count;
            foreach (var item in totalRecords)
            {                
                IDictionary<string, object> propertyValues = item;                
                foreach (var fields in propertyValues.Keys)
                {
                    var value = propertyValues[fields];
                    if (value != null && !columnTypes.ContainsKey(fields))
                    {

                        Type type = value.GetType();
                        if (type.IsValueType && nullable)
                        {
                            type = typeof(Nullable<>).MakeGenericType(type);
                        }
                        columnTypes.Add(fields, type);

                    }
                    else
                    {
                        columnTypes.Add(fields, typeof(object));
                    }
                }
                if (columnTypes.Count == propertyValues.Keys.Count)
                {
                    break;
                }
            }
            return columnTypes;
        }

        internal static string GetODataUrlKey(object rowData, string keyField, object value = null, Type ModelType = null)
        {
            var keyVal = value ?? GetObject(keyField, rowData);
            if (keyVal?.GetType() == typeof(string))
            {
                if ((ModelType != typeof(string)) && (Guid.TryParse((string)keyVal, out var newGuid) || int.TryParse((string)keyVal, out var newint) || decimal.TryParse((string)keyVal, out var newdecimal) || (keyVal?.GetType() == null)
                 || double.TryParse((string)keyVal, out var newdouble)))
                {
                    return $"({keyVal})";
                }
                else if (ModelType != typeof(string) && DateTime.TryParse((string)keyVal, out var newdatetime))
                {
                    if (Regex.IsMatch(keyVal.ToString(), @"(Z|[+-]\d{2}:\d{2})$"))
                    {
                        keyVal = DateTimeOffset.Parse((string)keyVal, CultureInfo.InvariantCulture);
                        return $"({((DateTimeOffset)keyVal).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)})";
                    }
                    else
                    {
                        keyVal = Convert.ToDateTime(keyVal, CultureInfo.InvariantCulture);
                        return $"({((DateTime)keyVal).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)})";
                    }
                }
                else
                {
                    return $"('{keyVal}')";
                }
            }
            else if (keyVal?.GetType() == typeof(DateTime))
            {
                return $"({((DateTime)keyVal).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)})";
            }
            else if (keyVal?.GetType() == typeof(DateTimeOffset))
            {
                return $"({((DateTimeOffset)keyVal).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)})";
            }
            else
            {
                return $"({keyVal})";
            }
        }

        /// <summary>
        /// Gets the property value from the DynamicObject.
        /// </summary>
        /// <param name="obj">Input dynamic object.</param>
        /// <param name="name">Property name to get.</param>
        /// <returns>object.</returns>
        public static object GetDynamicValue(DynamicObject obj, string name)
        {
            object value = null;
            //obj?.TryGetMember(new DataMemberBinder(name, false), out value);
            return value;
        }

        /// <summary>
        /// Gets the property value from the ExpandoObject.
        /// </summary>
        /// <param name="obj">Input Expando object.</param>
        /// <param name="name">Property name to get.</param>
        /// <returns>object.</returns>
        public static object GetExpandoValue(IDictionary<string, object> obj, string name)
        {
            object value = null;
            obj?.TryGetValue(name, out value);

            return value;
        }

        internal static object UpdateDictionary(IEnumerable<object> ExpandData, string[] columns)
        {
            List<IDictionary<string, object>> DicData = new List<IDictionary<string, object>>();

            //// this.DataHashTable.Clear();
            //IDictionary<string, object> DicValue;
            //if (ExpandData != null && !ExpandData.AsQueryable().Any())
            //{
            //    return null;
            //}

            //PropertyInfo[] props = ExpandData?.First()?.GetType().GetProperties();

            //foreach (var obj in ExpandData)
            //{
            //    string guid = System.Guid.NewGuid().ToString();
            //    var dynamicObj = obj as DynamicObject;
            //    if (dynamicObj != null)
            //    {
            //        dynamicObj.TrySetMember(new DataSetMemberBinder("BlazId", false), "BlazTempId_" + guid);
            //        var rowDataHolder = new Dictionary<string, object>();
            //        foreach (var col in columns)
            //        {
            //            dynamicObj.TryGetMember(new DataMemberBinder(col, false), out var value);
            //            rowDataHolder.Add(col, value);
            //        }

            //        DicData.Add(rowDataHolder);
            //    }
            //    else if (obj is ExpandoObject)
            //    {
            //        DicValue = (IDictionary<string, object>)obj;
            //        DicValue.AddOrUpdateDataItem("BlazId", "BlazTempId_" + guid);
            //        DicData.Add(DicValue);
            //    }
            //    else
            //    {
            //        DicValue = ObjectToDictionary(obj, props);
            //        DicValue.AddOrUpdateDataItem("BlazId", "BlazTempId_" + guid);
            //        DicData.Add(DicValue);
            //    }

            //    // this.DataHashTable.Add("BlazTempId_" + guid, obj);
            //}
            return DicData.Any() ? DicData : ExpandData;
        }

        internal static IDictionary<string, object> ObjectToDictionary(object o, PropertyInfo[] props)
        {
            IDictionary<string, object> res = new Dictionary<string, object>();
            for (int i = 0; i < props.Length; i++)
            {
                if (props[i].CanRead &&
                    (!Attribute.IsDefined(props[i], typeof(JsonIgnoreAttribute)) && !Attribute.IsDefined(props[i], typeof(System.Text.Json.Serialization.JsonIgnoreAttribute))))
                {
                    res.AddOrUpdateDataItem(props[i].Name, props[i].GetValue(o, null));
                }
            }

            return res;
        }
    }

    internal static class DataUtilExtension
    {
        internal static void AddOrUpdateDataItem(this IDictionary<string, object> dict, string key, object value)
        {
            if (dict.ContainsKey(key))
            {
                dict[key] = value;
            }
            else
            {
                dict.Add(key, value);
            }
        }
    }

}
