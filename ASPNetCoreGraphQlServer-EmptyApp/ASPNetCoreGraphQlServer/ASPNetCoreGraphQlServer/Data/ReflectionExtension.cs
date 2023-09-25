using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;

namespace ASPNetCoreGraphQlServer.Data
{
    /// <summary>
    /// An extension class which provides various extension methods to reflect the data from an object.
    /// </summary>
    /// <exclude/>
    public static class ReflectionExtension
    {
        /// <summary>
        /// Returns the property value of a specified object of any type includes static types, <see cref="DynamicObject"/> and <see cref="ExpandoObject"/> types.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="reflectComplexProperty">true, if need to reflect complex property. </param>
        /// <returns>The property value of the specified object. Also, returns null if <paramref name="obj"/> and <paramref name="propertyName"/> is null or empty.</returns>
        /// <remarks>For accessing complex/nested property value, given the propertyName with field names delimited by dot(.).</remarks>
        public static object GetValue(object obj, string propertyName, bool reflectComplexProperty = true)
        {
            if (string.IsNullOrEmpty(propertyName) || obj == null)
            {
                return null;
            }

            if (!reflectComplexProperty || !propertyName.Contains('.', StringComparison.InvariantCulture))
                return GetValueForDirectProperty(obj, propertyName);

            string[] splits = propertyName.Split('.');
            object value = obj;
            for (int i = 0; i < splits.Length; i++)
            {
                value = GetValueForDirectProperty(value, splits[i]);
                if (value == null)
                    break;
            }

            return value;
        }

        /// <summary>
        /// Returns true, the property value of a specified object of any type includes static types, 
        /// <see cref="DynamicObject"/> and <see cref="ExpandoObject"/> type is set to value parameter.
        /// </summary>
        public static bool TryGetValue(object obj, string propertyName, bool reflectComplexProperty, out object value)
        {
            if (string.IsNullOrEmpty(propertyName) || obj == null)
            {
                value = null;
                return false;
            }

            if (!reflectComplexProperty || !propertyName.Contains('.', StringComparison.InvariantCulture))
            {
                var dataObjectType = obj.GetType();

                var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(dataObjectType);
                if (isdyanmic)
                {
                    var expandoObject = obj as ExpandoObject;
                    if (expandoObject != null)
                    {
                        return (expandoObject as IDictionary<string, object>).TryGetValue(propertyName, out value);
                    }

                    var dynamicObject = obj as DynamicObject;
                    if (dynamicObject != null)
                    {
                        return dynamicObject.TryGetMember(new DataMemberBinder(propertyName, false), out value);
                    }
                }

                var pinfo = dataObjectType.GetProperty(propertyName);
                if (pinfo != null)
                {
                    value = pinfo.GetValue(obj);
                    return true;
                }
                value = null;
                return false;
            }
            
            string[] splits = propertyName.Split('.');
            value = obj;
            for (int i = 0; i < splits.Length; i++)
            {
                if (!TryGetValue(value, propertyName, false, out value))
                {
                    value = null;
                    return false;
                }
                if (value == null)
                    return true;
            }

            return true;
        }

        private static object GetValueForDirectProperty(object obj, string propertyName)
        {
            var dataObjectType = obj.GetType();

            var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(dataObjectType);
            if (isdyanmic) 
            {
               return GetValueFromIDynamicMetaObject(obj, propertyName);
            }

            var pinfo = dataObjectType.GetProperty(propertyName);
            if (pinfo != null)
            {
                return pinfo.GetValue(obj);
            }
            
            return dataObjectType.GetField(propertyName)?.GetValue(obj);
        }

        /// <summary>
        /// Returns the property value of a specified object of type s<see cref="DynamicObject"/>.
        /// </summary>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <returns>The property value of the specified object.</returns>
        public static object GetValueFromStaticObject(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            var dataObjectType = obj.GetType();
            var pInfo = dataObjectType.GetProperty(propertyName);
            if (pInfo != null)
            {
                return pInfo.GetValue(obj);
            }

            return dataObjectType.GetField(propertyName)?.GetValue(obj);
        }

        private static bool SetValueForDirectProperty(object obj, string propertyName, object value, SetOptions options = null)
        {
            if (obj == null)
                return false;

            try
            {
                var dataObjectType = obj.GetType();

                var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(dataObjectType);
                if (isdyanmic)
                {
                    SetValueToIDynamicMetaObject(obj, propertyName, value);
                }

                var propInfo = dataObjectType.GetProperty(propertyName);
                if (propInfo == null || !propInfo.CanWrite || propInfo.SetMethod == null)
                    return false;

                //else
                if (propInfo.PropertyType.Name == "Guid" ||
                    Nullable.GetUnderlyingType(propInfo.PropertyType)?.Name == "Guid")
                {
                    if (value == null || string.IsNullOrEmpty(value.ToString()))
                    {
                        if (value == null && propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                            propInfo.SetValue(obj, null);
                        else
                            propInfo.SetValue(obj, Guid.Empty);
                        return true;
                    }
                    if (Guid.TryParse(value.ToString(), out var newGuid))
                    {
                        propInfo.SetValue(obj, newGuid);
                        return true;
                    }
                }
                else if (options != null && options.CanConvertToPropertyType)
                    value = Convert.ChangeType(value, propInfo.PropertyType);

                propInfo.SetValue(obj, value);
                return true;
            }
#pragma warning disable CS0168, CA1031
            catch (Exception ex)
#pragma warning restore CS0168, CA1031
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object.
        /// </summary>
        /// <param name="obj">The object whose property value will set.</param>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="value">The new property value.</param>
        /// <param name="reflectComplexProperty">true, if need to reflect complex property. </param>
        /// <param name="options">Options to control set options. </param>
        public static bool SetValue(object obj, string propertyName, object value, 
            bool reflectComplexProperty = true, SetOptions options = null)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return false;

            if (options == null)
                options = SetOptions.Default;

            try
            {
                if (!reflectComplexProperty || !propertyName.Contains('.', StringComparison.InvariantCulture))
                {
                    return SetValueForDirectProperty(obj, propertyName, value);
                }
                //else
                string[] splits = propertyName.Split('.');
                object propertyobj = obj;
                for (int i = 0; i < splits.Length; i++)
                {
                    if (propertyobj == null)
                        return false;

                    if (i == splits.Length - 1)
                    {
                        return SetValueForDirectProperty(propertyobj, splits[i], value, options);
                    }
                    //else
                    object tempobj;
                    if (!TryGetValue(propertyobj, splits[i], false, out tempobj))
                    {
                        return false;
                    }
                    //else
                    if (tempobj == null)
                    {
                        var propertyObjectType = propertyobj.GetType();
                        var propertyInfo = propertyObjectType.GetProperty(splits[i]);

                        var propertyType = propertyInfo.PropertyType;
                        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                        {
                            var underlyingType = Nullable.GetUnderlyingType(propertyType);
                            if (underlyingType != null)
                            {
                                propertyType = underlyingType;
                            }
                        }

                        var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(propertyInfo.PropertyType);
                        if (!isdyanmic && !propertyType.IsPrimitive && !propertyType.IsEnum
                            && !propertyType.IsAbstract)
                        {
                            tempobj = ReflectionExtension.TryCreateInstance(propertyType);
                            if (tempobj != null)
                            {
                                if (!SetValueForDirectProperty(propertyobj, splits[i], tempobj))
                                    return false;
                            }
                        }
                    }
                    propertyobj = tempobj;   
                }
                return false;
                
            }
#pragma warning disable CS0168, CA1031
            catch (Exception ex)
#pragma warning restore CS0168, CA1031
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the property value of a specified object of type <see cref="DynamicObject"/> or <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="reflectComplexProperty">true, if need to reflect complex property. </param>
        /// <returns>The property value of the specified object.</returns>
        public static object GetValueFromIDynamicMetaObject(object obj, string propertyName, bool reflectComplexProperty = false)
        {
            var expandoObject = obj as ExpandoObject;
            if (expandoObject != null)
            {
                return GetValueFromExpandoObject(expandoObject, propertyName, reflectComplexProperty);
            }

            var dynamicObject = obj as DynamicObject;
            if (dynamicObject != null)
            {
                return GetValueFromDynamicObject(dynamicObject, propertyName, reflectComplexProperty);
            }

            throw new NotImplementedException(obj?.GetType().Name??"obj is null");
        }

        /// <summary>
        /// Returns the property value of a specified object of type <see cref="DynamicObject"/>.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="reflectComplexProperty">true, if need to reflect complex property. </param>
        /// <returns>The property value of the specified object.</returns>
        public static object GetValueFromDynamicObject(DynamicObject obj, string propertyName, bool reflectComplexProperty = false)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return false;

            object value;

            if (!reflectComplexProperty || !propertyName.Contains('.', StringComparison.InvariantCulture))
                obj.TryGetMember(new DataMemberBinder(propertyName, false), out value);
            else
            {
                string[] splits = propertyName.Split('.');
                value = obj;
                for (int i = 0; i < splits.Length; i++)
                {
                    if (i == 0)
                    {
                        if (!obj.TryGetMember(new DataMemberBinder(splits[i], false), out value))
                            value = null;
                    }
                    else
                    {
                        value = GetValueForDirectProperty(value, splits[i]);
                    }
                    if (value == null)
                        return null;
                }
            }
            return value;
        }

        /// <summary>
        /// Returns the property value of a specified object of type <see cref="DynamicObject"/> and <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="value">The new property value.</param>
        public static bool SetValueToIDynamicMetaObject(object obj, string propertyName, object value)
        {
            var expandoObject = obj as ExpandoObject;
            if (expandoObject != null)
            {
                return SetValueToExpandoObject(expandoObject, propertyName, value);
            }

            var dynamicObject = obj as DynamicObject;
            if (dynamicObject != null)
            {
                return SetValueToDynamicObject(dynamicObject, propertyName, value);
            }
            throw new NotImplementedException(obj?.GetType().Name ?? "obj is null");
        }

        /// <summary>
        /// Returns the property value of a specified object of type <see cref="DynamicObject"/>.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="value">The new property value.</param>
        public static bool SetValueToDynamicObject(DynamicObject obj, string propertyName, object value)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return false;

            try
            {
                obj.TrySetMember(new DataSetMemberBinder(propertyName, false), value);
                return true;
            }
#pragma warning disable CS0168, CA1031
            catch (Exception ex)
#pragma warning restore CS0168, CA1031
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the property value of a specified object of type <see cref="ExpandoObject"/>.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <param name="reflectComplexProperty">true, if need to reflect complex property. </param>
        /// <returns>The property value of the specified object.</returns>
        public static object GetValueFromExpandoObject(IDictionary<string, object> obj, string propertyName, bool reflectComplexProperty = false)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return null;

            object value = null;
            if (!reflectComplexProperty || !propertyName.Contains('.', StringComparison.InvariantCulture))
                obj.TryGetValue(propertyName, out value);
            else
            {
                string[] splits = propertyName.Split('.');
                for (int i = 0; i < splits.Length; i++)
                {
                    if (i == 0)
                    {
                        if (!obj.TryGetValue(splits[i], out value))
                            value = null;
                    }
                    else
                    {
                        value = GetValueForDirectProperty(value, splits[i]);
                    }
                    if (value == null)
                        return null;
                }
            }
            return value;
        }

        /// <summary>
        /// Sets the property value of a specified object.
        /// </summary>
        /// <param name="propertyName">The string containing the name of the public property.</param>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <returns>The property value of the specified object.</returns>
        /// <param name="value">The new property value.</param>
        public static bool SetValueToExpandoObject(IDictionary<string, object> obj, string propertyName, object value)
        {
            if (obj == null)
                return false;

            try
            {
                obj[propertyName] = value;
                return true;
            }
#pragma warning disable CS0168, CA1031
            catch (Exception ex)
#pragma warning restore CS0168, CA1031
            {
                return false;
            }
        }

        /// <summary>
        /// Creates an instance of the specified type using that type's parameterless constructor.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="createsubtypes">true, if nested properties also should be initialized with instance</param>
        /// <returns>A reference to the newly created object.</returns>
        public static object TryCreateInstance<T>(bool createsubtypes = false)
        {
            return TryCreateInstance(typeof(T), createsubtypes);
        }

        /// <summary>
        /// Creates an instance of the specified type using that type's parameterless constructor.
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <param name="createsubtypes">true, if nested properties also should be initialized with instance.</param>
        /// <returns>A reference to the newly created object.</returns>
        public static object TryCreateInstance(Type type, bool createsubtypes = false)
        {
            try
            {
                var obj = Activator.CreateInstance(type);
                if (!createsubtypes || obj == null)
                    return obj;

                var propertiesinfos = obj.GetType().GetProperties();
                foreach (PropertyInfo properinfo in propertiesinfos)
                {
                    var isdyanmic = typeof(IDynamicMetaObjectProvider).IsAssignableFrom(properinfo.PropertyType);
                    if (isdyanmic || properinfo.PropertyType.IsPrimitive || properinfo.PropertyType.IsEnum || 
                        properinfo.PropertyType.IsAbstract || !properinfo.CanWrite || properinfo.SetMethod == null)
                        continue;

                    var propertyValue = TryCreateInstance(properinfo.PropertyType, createsubtypes);
                    if (propertyValue != null)
                    {
                        properinfo.SetValue(obj, propertyValue);
                    }
                }
                return obj;
            }
            catch
            {
                return null;
                throw;
            }
        }
    }

    /// <summary>
    /// An extension class which provides various extension methods to clone an object.
    /// </summary>
    /// <exclude/>
    public static class CloneUtils
    {
        /// <summary>
        /// Creates and returns a new object that is a copy of the specified <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object whose clone will be returned.</param>
        /// <param name="type">Type of new object type.</param>
        /// <returns>A new object that is a copy of <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> can't be null.</exception>
        public static object Clone(object obj, Type type)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Original obj can't be null in Clone method");

            var expandoObj = obj as ExpandoObject;

            if (expandoObj != null)
            {
                return CloneExpandoObject(expandoObj);
            }

            var dynamicObj = obj as DynamicObject;
            if (obj is DynamicObject)
            {
                return CloneDynamicObject(dynamicObj, type);
            }

            return CloneStaticObjectType(obj, type);
        }

        /// <summary>
        /// Creates and returns a new object that is a copy of the specified <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object whose clone will be returned.</param>
        /// <param name="type">Type of new object type.</param>
        /// <param name="PreventDataClone">To prevent the object cloning.</param>
        /// <returns>A new object that is a copy of <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> can't be null.</exception>
        public static object CloneStaticObjectType(object obj, Type type, bool PreventDataClone = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Original obj can't be null in Clone method");

            var cloneobj = ReflectionExtension.TryCreateInstance(type ?? obj.GetType());
            if (cloneobj == null)
            {
                return null;
            }

            if (PreventDataClone)
            {
                return obj;
            }

            var properties = obj.GetType().GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite || property.SetMethod == null)
                    continue;

                var propertyValue = property.GetValue(obj);
                var propertyType = property.PropertyType;

                if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                    if(underlyingType != null)
                    {
                        propertyType = underlyingType;
                    }
                }
                
                if (propertyValue == null || propertyType.IsPrimitive || propertyType.IsEnum || propertyType.IsAbstract || propertyValue is string || 
                    propertyValue is TimeSpan || propertyValue is decimal || propertyValue is DateTime || propertyValue is DateOnly || propertyValue is TimeOnly || propertyValue is IEnumerable || 
                    propertyValue is DateTimeOffset || propertyValue is ICollection || propertyValue is Guid)
                {
                    ReflectionExtension.SetValue(cloneobj, property.Name, propertyValue, false);
                }
                else
                {
                    var clonedvalue = Clone(propertyValue, property.PropertyType);
                    if (clonedvalue != null)
                        ReflectionExtension.SetValue(cloneobj, property.Name, clonedvalue, false);
                    else
                        ReflectionExtension.SetValue(cloneobj, property.Name, propertyValue, false);
                    //Kanban, Schedule checked and cloned dynamic and expando in static types too. So above code used.
                    //ReflectionExtension.SetValue(cloneobj, property.Name, CloneStaticObjectType(propertyValue, property.PropertyType), false);
                }
            }

            return cloneobj;
        }

        /// <summary>
        /// Creates and returns a new object that is a copy of the specified <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object whose clone will be returned.</param>
        /// <param name="PreventDataClone">To prevent the object cloning.</param>
        /// <returns>A new object that is a copy of <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> can't be null.</exception>
        public static ExpandoObject CloneExpandoObject(ExpandoObject obj, bool PreventDataClone = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Original obj can't be null in CloneExpandoObject method");
            if (PreventDataClone)
                return obj;
            var cloneobj = new ExpandoObject();

            var _original = (IDictionary<string, object>)obj;
            var _clone = (IDictionary<string, object>)cloneobj;

            foreach (var item in _original)
            {
                _clone.Add(item);

                var subvalue = item.Value as ExpandoObject;
                if (subvalue != null)
                {
                    _clone[item.Key] = CloneExpandoObject(subvalue);
                }
            }

            return cloneobj;
        }

        /// <summary>
        /// Creates and returns a new object that is a copy of the specified <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object whose clone will be returned.</param>
        /// <param name="type">Type of new object type.</param>
        /// <param name="PreventDataClone">To prevent the object cloning.</param>
        /// <returns>A new object that is a copy of <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> can't be null.</exception>
        public static DynamicObject CloneDynamicObject(DynamicObject obj, Type type = null, bool PreventDataClone = false)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Original obj can't be null in CloneDynamicObject method");

            var cloneobj = (DynamicObject)ReflectionExtension.TryCreateInstance(type ?? obj.GetType());
            if (cloneobj == null)
                return null;
            if (PreventDataClone)
                return obj;
            var properties = obj.GetDynamicMemberNames();
            foreach (var property in properties)
            {
                var subvalue = ReflectionExtension.GetValueFromDynamicObject(obj, property);
                ReflectionExtension.SetValueToDynamicObject(cloneobj, property, subvalue);
            }

            return cloneobj;
        }
    }

    /// <summary>
    /// Provides options on conversion, complex property handling when set the property value of a object.
    /// </summary>
    /// <exclude/>
    public class SetOptions
    {
        internal static SetOptions Default = new SetOptions();

        /// <summary>
        /// Gets or sets whether to create new instances for setting complex property values.
        /// Not applicable for <see cref="ExpandoObject"/> and <see cref="DynamicObject"/>.
        /// </summary>
        public bool CreateInstanceForComplexType { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to convert the value to 
        /// <see cref="System.Reflection.PropertyInfo.PropertyType"/> before set the value.
        /// </summary>
        public bool CanConvertToPropertyType { get; set; }
    }

    /// <summary>
    /// Defines the data member binder for getting dynamic object property.
    /// </summary>
    /// <exclude/>
    internal class DataMemberBinder : GetMemberBinder
    {
        public DataMemberBinder(string name, bool ignoreCase)
            : base(name, ignoreCase)
        {
        }

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Defines the data member binder for setting dynamic object property.
    /// </summary>
    /// <exclude/>
    internal class DataSetMemberBinder : SetMemberBinder
    {
        public DataSetMemberBinder(string name, bool ignoreCase)
            : base(name, ignoreCase)
        {
        }

        public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }
    }
}
