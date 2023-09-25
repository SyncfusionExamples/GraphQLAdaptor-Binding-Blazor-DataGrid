namespace ASPNetCoreGraphQlServer.Data
{
    using System;
    using System.ComponentModel;
    using System.Reflection;
    using System.IO;
    using System.Globalization;
    using System.Collections;
    using Hashtable = System.Collections.Generic.Dictionary<object, object>;

    /// <summary>
    /// A framework independent utility class for the new Nullable type in .NET Framework 2.0.
    /// </summary>
    /// <exclude/>
    public static class NullableHelperInternal
    {
#if EJ2_DNX
        /// <summary>
        /// Indicates whether the specified PropertyDescriptor has nested properties.
        /// </summary>
        /// <param name="pd">The PropertyDescriptor to be checked.</param>
        /// <returns>True if nested properties are found; False otherwise.</returns>
        internal static bool IsComplexType(PropertyDescriptor pd)
        {
            if (pd.ComponentType == typeof(Type))
                return false;

            Type t = pd.PropertyType;
            return IsComplexType(t);
        }
#else
        /// <summary>
        /// Indicates whether the specified PropertyDescriptor has nested properties.
        /// </summary>
        /// <param name="pd">The PropertyDescriptor to be checked.</param>
        /// <returns>True if nested properties are found; False otherwise.</returns>
        internal static bool IsComplexType(PropertyInfo pd)
        {
            Type t = pd.PropertyType;
            return IsComplexType(t);
        }
#endif

        /// <summary>
        /// Indicates whether the specified Type has nested properties.
        /// </summary>
        /// <param name="t">The Type to be checked.</param>
        /// <returns>True if nested properties are found; False otherwise.</returns>
        public static bool IsComplexType(Type t)
        {
            Type underlyingType = NullableHelperInternal.GetUnderlyingType(t);
            if (underlyingType != null)
            {
                t = underlyingType;
            }
#if EJ2_DNX
            if (t != typeof(object)
                && t != typeof(Decimal)
                && t != typeof(DateTime)
                && t != typeof(Type)
                && t != typeof(string)
                && t != typeof(Guid)
                && t.BaseType != typeof(Enum)
                && !t.IsPrimitive)
#else
            if (t != typeof(object)
                && t != typeof(decimal)
                && t != typeof(DateTime)
                && t != typeof(Type)
                && t != typeof(string)
                && t != typeof(Guid)
                && t.GetTypeInfo().BaseType != typeof(Enum)
                && !t.GetTypeInfo().IsPrimitive)
#endif
                return true;

            return false;
        }

#if EJ2_DNX
        public static bool IsIEnumerableType(PropertyDescriptor pd)
#else
        public static bool IsIEnumerableType(PropertyInfo pd)
#endif
        {
#if EJ2_DNX
 if (NullableHelperInternal.IsComplexType(pd.PropertyType)
                && !typeof(byte[]).IsAssignableFrom(pd.PropertyType)
                && pd.PropertyType != typeof(string)
                && typeof(IEnumerable).IsAssignableFrom(pd.PropertyType)
                && !(pd.PropertyType.IsArray && pd.PropertyType.GetElementType().IsPrimitive))
#else
            if (pd != null && NullableHelperInternal.IsComplexType(pd.PropertyType)
            && !typeof(byte[]).IsAssignableFrom(pd.PropertyType)
            && pd.PropertyType != typeof(string)
            && typeof(IEnumerable).IsAssignableFrom(pd.PropertyType)
            && !(pd.PropertyType.IsArray && pd.PropertyType.GetElementType().GetTypeInfo().IsPrimitive))
#endif
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Use this method instead of Convert.ChangeType. Makes Convert.ChangeType work with Nullable types.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type type)
        {
            Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullableUnderlyingType != null)
            {
                if (value is string && nullableUnderlyingType != typeof(string))
                {
                    if (ValueConvert.IsEmpty((string)value))
                    {
                        return null;
                    }
                }

                value = ChangeType(value, nullableUnderlyingType, CultureInfo.InvariantCulture);
                if (value is DBNull)
                {
                    return null;
                }

                return value;
            }
#if EJ2_DNX
            if (!type.IsInterface)
#else
            if (!type.GetTypeInfo().IsInterface)
#endif
                return TypeConverterHelper.ChangeType(value, type, CultureInfo.InvariantCulture);
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Use this method instead of Convert.ChangeType. Makes Convert.ChangeType work with Nullable types.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static object ChangeType(object value, Type type, IFormatProvider provider)
        {
            Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullableUnderlyingType != null)
            {
                if (value is string && nullableUnderlyingType != typeof(string))
                {
                    if (ValueConvert.IsEmpty((string)value))
                    {
                        return null;
                    }
                }

                value = ChangeType(value, nullableUnderlyingType, provider);
                if (value is DBNull)
                {
                    return null;
                }

                return value;
            }

            return TypeConverterHelper.ChangeType(value, type, provider);
        }

        public static bool IsNullableType(Type nullableType)
        {
            if (nullableType == null)
            {
                throw new ArgumentNullException(nameof(nullableType));
            }

            bool result = false;
#if EJ2_DNX
            if ((nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition) && (nullableType.GetGenericTypeDefinition() == typeof(Nullable<>)))
#else
            if ((nullableType.GetTypeInfo().IsGenericType && !nullableType.GetTypeInfo().IsGenericTypeDefinition) && (nullableType.GetGenericTypeDefinition() == typeof(Nullable<>)))
#endif
            {
                result = true;
            }

            return result;
        }

        public static Type GetNullableType(Type type)
        {
            if (type == null)
            {
                return null;
            }

            if (NullableHelperInternal.IsNullableType(type))
            {
                return type;
            }

            var underlyingType = Nullable.GetUnderlyingType(type);
            underlyingType = underlyingType ?? type;

#if EJ2_DNX
            if (underlyingType.IsValueType)
#else
            if (underlyingType.GetTypeInfo().IsValueType)
#endif
                return typeof(Nullable<>).MakeGenericType(type);
            return type;
        }

        /// <summary>
        /// Returns null if value is DBNull and specified type is a Nullable type. Otherwise the value is returned unchanged.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object FixDbNUllasNull(object value, Type type)
        {
            if (type == null)
            {
                return value;
            }

            Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullableUnderlyingType != null)
            {
                if (value is DBNull)
                {
                    return null;
                }
            }

            /*
             * Do not return DBNull for strong typed properties of an object. For example, if Parsing a string failed
             * (e.g. if an empty string was passed in as argument) we need to check if it as object and in that
             * case return null. Only if it is a ValueType type (that is not nullable) then we should return DBNull
             * so that it also works with DataRowView.
             * */
#if EJ2_DNX
            if (!type.IsValueType)
#else
            if (!type.GetTypeInfo().IsValueType)
#endif
            {
                if (value is DBNull)
                {
                    return null;
                }
            }

            return value;
        }

        /// <summary>
        /// Returns the underlying type of a Nullable type. For .NET 1.0 and 1.1 this method will always return null.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetUnderlyingType(Type type)
        {
            return type == null ? null : Nullable.GetUnderlyingType(type);
        }

        /// <exclude/>
        private class TypeConverterHelper
        {
            public static object ChangeType(object value, Type type)
            {
                return ChangeType(value, type, null);
            }

            public static object ChangeType(object value, Type type, IFormatProvider provider)
            {
                // Fix for defects: 13036, 13024, 12601  & 12716
                if (value == null)
                {
                    return null;
                }

                TypeConverter typeConverter = TypeDescriptor.GetConverter(value.GetType());
                if (typeConverter != null && typeConverter.CanConvertTo(type))
                {
                    return typeConverter.ConvertTo(value, type);
                }

                if (value is DBNull)
                {
                    return DBNull.Value;
                }
#if EJ2_DNX
                if (type.IsEnum)
                {
                    return Enum.Parse(type, Convert.ToString(value));
                }
#endif
                return Convert.ChangeType(value, type, provider);
            }
        }
    }

    /// <summary>
    /// <see cref="ValueConvert"/> provides conversion routines for values
    /// to convert them to another type and routines for formatting values.
    /// </summary>
    public static class ValueConvert
    {
        /// <overload>
        /// Converts value from one type to another using an optional <see cref="IFormatProvider"/>.
        /// </overload>
        /// <summary>
        /// Converts value from one type to another using an optional <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="type">The target type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value.</param>
        /// <returns>The new value in the target type.</returns>
        public static object ChangeType(object value, Type type, IFormatProvider provider)
        {
            return ChangeType(value, type, provider, false);
        }

        /// <summary>
        /// Converts value from one type to another using an optional <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="type">The target type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value.</param>
        /// <param name="returnDbNUllIfNotValid">Indicates whether exceptions should be avoided or catched and return value should be DBNull if
        /// it cannot be converted to the target type.</param>
        /// <returns>The new value in the target type.</returns>
        public static object ChangeType(object value, Type type, IFormatProvider provider, bool returnDbNUllIfNotValid)
        {
            return ChangeType(value, type, provider, string.Empty, returnDbNUllIfNotValid);
        }

        /// <summary>
        /// Converts value from one type to another using an optional <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="type">The target type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value.</param>
        /// <param name="format">Format string.</param>
        /// <param name="returnDbNUllIfNotValid">Indicates whether exceptions should be avoided or catched and return value should be DBNull if
        /// it cannot be converted to the target type.</param>
        /// <returns>The new value in the target type.</returns>
        public static object ChangeType(object value, Type type, IFormatProvider provider, string format,
                                        bool returnDbNUllIfNotValid)
        {
            Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            if (nullableUnderlyingType != null)
            {
                value = ChangeType(value, nullableUnderlyingType, provider, true);
                return NullableHelperInternal.FixDbNUllasNull(value, type);
            }

            if (value != null && type != null && !type.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    if (value is string)
                    {
                        if (format != null && format.Length > 0)
                        {
                            value = Parse((string)value, type, provider, format, returnDbNUllIfNotValid);
                        }
                        else
                        {
                            value = Parse((string)value, type, provider, string.Empty, returnDbNUllIfNotValid);
                        }
                    }
                    else if (value is System.DBNull)
                    {
                        // value = null; changed after 4.1.0.50: do not set it to null - this causes then issues
                        // if you have a DataTable and the key is used for lookups, e.g.
                        // see sample in http://www.syncfusion.com/support/forums/message.aspx?MessageID=40207
                        // For NullableTypes the above call to NullableHelper.FixDbNUllasNull will
                        // take care of converting DbNull to null for nullable types only.
                    }
#if EJ2_DNX
                    else if (type.IsEnum)
#else
                    else if (type.GetTypeInfo().IsEnum)
#endif
                    {
                        value = Convert.ChangeType(value, typeof(int), provider);
                        value = Enum.ToObject(type, (int)value);
                    }
                    else if (type == typeof(string) && !(value is IConvertible))
                    {
                        value = value.ToString();
                    }
                    else
                    {
                        value = NullableHelperInternal.ChangeType(value, type, provider);
                    }
                }
                catch
                {
                    if (returnDbNUllIfNotValid)
#if EJ2_DNX
                        return Convert.DBNull;
#else
                        return System.DBNull.Value;
#endif
                    throw;
                }
            }

            if ((value == null || value is DBNull) && type == typeof(string))
            {
                return null;
            }

            return value;
        }

        private static Hashtable cachedDefaultValues = new Hashtable();

        /// <summary>
        /// Overloaded. Parses the given text using the resultTypes "Parse" method or using a type converter.
        /// </summary>
        /// <param name="s">The text to parse.</param>
        /// <param name="resultType">The requested result type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value. Can be NULL.</param>
        /// <returns>The new value in the target type.</returns>
        private static object Parse(string s, Type resultType, IFormatProvider provider)
        {
            return Parse(s, resultType, provider, string.Empty);
        }

        /// <summary>
        /// Parses the given text using the resultTypes "Parse" method or using a type converter.
        /// </summary>
        /// <param name="s">The text to parse.</param>
        /// <param name="resultType">The requested result type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value. Can be NULL.</param>
        /// <param name="format">A format string used in a <see cref="object.ToString"/> call. Right now
        /// format is only interpreted to enable roundtripping for formatted dates.
        /// </param>
        /// <returns>The new value in the target type.</returns>
        public static object Parse(string s, Type resultType, IFormatProvider provider, string format)
        {
            return Parse(s, resultType, provider, format, false);
        }

        /// <summary>
        /// Parse the given text using the resultTypes "Parse" method or using a type converter.
        /// </summary>
        /// <param name="s">The text to parse.</param>
        /// <param name="resultType">The requested result type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value. Can be NULL.</param>
        /// <param name="format">A format string used in a <see cref="object.ToString"/> call. Right now
        /// format is only interpreted to enable roundtripping for formatted dates.
        /// </param>
        /// <param name="returnDbNUllIfNotValid">Indicates whether DbNull should be returned if value cannot be parsed. Otherwise an exception is thrown.</param>
        /// <returns>The new value in the target type.</returns>
        public static object Parse(string s, Type resultType, IFormatProvider provider, string format,
                                   bool returnDbNUllIfNotValid)
        {
            object value = _Parse(s, resultType, provider, format, returnDbNUllIfNotValid);
            return NullableHelperInternal.FixDbNUllasNull(value, resultType);
        }

        /// <summary>
        /// Parse the given text using the resultTypes "Parse" method or using a type converter.
        /// </summary>
        /// <param name="s">The text to parse.</param>
        /// <param name="resultType">The requested result type.</param>
        /// <param name="provider">A <see cref="IFormatProvider"/> used to format or parse the value. Can be NULL.</param>
        /// <param name="formats">A string array holding permissible formats used in a <see cref="object.ToString"/> call. Right now
        /// formats is only interpreted to enable roundtripping for formatted dates.
        /// </param>
        /// <param name="returnDbNUllIfNotValid">Indicates whether DbNull should be returned if value cannot be parsed. Otherwise an exception is thrown.</param>
        /// <returns>The new value in the target type.</returns>
        public static object Parse(string s, Type resultType, IFormatProvider provider, string[] formats,
                                   bool returnDbNUllIfNotValid)
        {
            object value = _Parse(s ?? string.Empty, resultType, provider, "", formats, returnDbNUllIfNotValid);
            return NullableHelperInternal.FixDbNUllasNull(value, resultType);
        }

        private static object _Parse(string s, Type resultType, IFormatProvider provider, string format,
                                     bool returnDbNUllIfNotValid)
        {
            return _Parse(s, resultType, provider, format, null, returnDbNUllIfNotValid);
        }

        private static object ParseDouble(string s, Type resultType, IFormatProvider provider, bool returnDbNUllIfNotValid)
        {
            object result = null;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif
            double d;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                result = Convert.ChangeType(d, resultType, provider);
                return result;
            }

            if (returnDbNUllIfNotValid)
            {
                if (resultType == typeof(double) || resultType == typeof(float))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                    return System.DBNull.Value;
#endif
            }
            return result;
        }

        private static object ParseDecimal(string s, Type resultType, IFormatProvider provider)
        {
            object result = null;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif
            decimal d;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                result = Convert.ChangeType(d, resultType, provider);
                return result;
            }
            return result;
        }

        private static object ParseDateTime(string s, Type resultType, IFormatProvider provider, string format,
                                     string[] formats, bool returnDbNUllIfNotValid)
        {
            _ = resultType;
            returnDbNUllIfNotValid = false;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif

            if (formats == null || formats.GetLength(0) == 0 && format.Length > 0)
                formats = new string[] { format, "G", "g", "f", "F", "d", "D" };

            if (formats.GetLength(0) > 0)
            {
                DateTime dtresult;
                if (DateTime.TryParseExact(s, formats, provider,
                                           DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowLeadingWhite |
                                           DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowWhiteSpaces,
                                           out dtresult))
                {
                    return dtresult;
                }
            }

            DateTime validDateTime;
            DateTime.TryParse(s, provider,
                              DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowLeadingWhite |
                              DateTimeStyles.AllowTrailingWhite |
                              DateTimeStyles.AllowWhiteSpaces, out validDateTime);
            return validDateTime;
        }

        private static object ParseTimeSpan(string s)
        {
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif
            var isValid = false;
            TimeSpan timespan;
            if (TimeSpan.TryParse(s, out timespan))
            {
                isValid = true;
            }

            if (isValid)
            {
                return timespan;
            }
            return timespan;
        }

        private static object ParseBool(string s)
        {
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif

            if (s == "1" || s.ToUpperInvariant() == bool.TrueString.ToUpperInvariant())
                return true;
            else if (s == "0" || s.ToUpperInvariant() == bool.FalseString.ToUpperInvariant())
            {
                return false;
            }
            return false;
        }

        private static object ParseLong(string s, Type resultType, IFormatProvider provider, bool returnDbNUllIfNotValid)
        {
            object result = null;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif

            long d;
            if (long.TryParse(s, NumberStyles.Any, provider, out d))
            {
                result = Convert.ChangeType(d, resultType, provider);
                return result;
            }

            if (returnDbNUllIfNotValid)
            {
#if EJ2_DNX
                        if (resultType.IsPrimitive && !resultType.IsEnum)
#else
                if (resultType.GetTypeInfo().IsPrimitive && !resultType.GetTypeInfo().IsEnum)
#endif
#if EJ2_DNX
                            return Convert.DBNull;
#else
                    return System.DBNull.Value;
#endif
            }
            return result;
        }

        private static object ParseULong(string s, Type resultType, IFormatProvider provider, bool returnDbNUllIfNotValid)
        {
            object result = null;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif

            ulong d;
            if (ulong.TryParse(s, NumberStyles.Any, provider, out d))
            {
                result = Convert.ChangeType(d, resultType, provider);
                return result;
            }

            if (returnDbNUllIfNotValid)
            {
#if EJ2_DNX
                        if (resultType.IsPrimitive && !resultType.IsEnum)
#else
                if (resultType.GetTypeInfo().IsPrimitive && !resultType.GetTypeInfo().IsEnum)
#endif
#if EJ2_DNX
                            return Convert.DBNull;
#else
                    return System.DBNull.Value;
#endif
            }
            return result;
        }

        private static object ParseNumber(string s, Type resultType, IFormatProvider provider, bool returnDbNUllIfNotValid)
        {
            object result = null;
            if (IsEmpty(s))
#if EJ2_DNX
                        return Convert.DBNull;
#else
                return System.DBNull.Value;
#endif

            double d;
            if (double.TryParse(s, NumberStyles.Any, provider, out d))
            {
                result = Convert.ChangeType(d, resultType, provider);
                return result;
            }

            if (returnDbNUllIfNotValid)
            {
#if EJ2_DNX
                        if (resultType.IsPrimitive && !resultType.IsEnum)
#else
                if (resultType.GetTypeInfo().IsPrimitive && !resultType.GetTypeInfo().IsEnum)
#endif
#if EJ2_DNX
                            return Convert.DBNull;
#else
                    return System.DBNull.Value;
#endif
            }
            return result;
        }

        private static object _Parse(string s, Type resultType, IFormatProvider provider, string format,
                                     string[] formats, bool returnDbNUllIfNotValid)
        {
            // Fix for defect #12619.
            if (resultType == null) // || resultType == typeof(string))
            {
                return s;
            }

            object result;

            try
            {
                if (typeof(double).IsAssignableFrom(resultType))
                {
                    return ParseDouble(s, resultType, provider, returnDbNUllIfNotValid);
                }
                else if (typeof(decimal).IsAssignableFrom(resultType))
                {
                    return ParseDecimal(s, resultType, provider);
                }
                else if (typeof(DateTime).IsAssignableFrom(resultType))
                {
                    return ParseDateTime(s, resultType, provider, format, formats, returnDbNUllIfNotValid);
                }
                else if (typeof(TimeSpan).IsAssignableFrom(resultType))
                {
                    return ParseTimeSpan(s);
                }
                else if (typeof(bool).IsAssignableFrom(resultType))
                {
                    return ParseBool(s);
                }
                else if (typeof(long).IsAssignableFrom(resultType))
                {
                    return ParseLong(s, resultType, provider, returnDbNUllIfNotValid);
                }
                else if (typeof(ulong).IsAssignableFrom(resultType))
                {
                    return ParseULong(s, resultType, provider, returnDbNUllIfNotValid);
                }
                else if (typeof(int).IsAssignableFrom(resultType)
                         || typeof(short).IsAssignableFrom(resultType)
                         || typeof(float).IsAssignableFrom(resultType)
                         || typeof(uint).IsAssignableFrom(resultType)
                         || typeof(ushort).IsAssignableFrom(resultType)
                         || typeof(byte).IsAssignableFrom(resultType))
                {
                    return ParseNumber(s, resultType, provider, returnDbNUllIfNotValid);
                }
                else if (resultType == typeof(Type))
                {
                    result = Type.GetType(s);
                    return result;
                }

                TypeConverter typeConverter = TypeDescriptor.GetConverter(resultType);

                if (typeConverter is NullableConverter)
                {
                    Type nullableUnderlyingType = NullableHelperInternal.GetUnderlyingType(resultType);
                    if (nullableUnderlyingType != null)
                    {
                        return _Parse(s, nullableUnderlyingType, provider, format, formats, returnDbNUllIfNotValid);
                    }
                }

                if (typeConverter != null &&
                    typeConverter.CanConvertFrom(typeof(string)) &&
                    s != null && s.Length > 0)
                {
                    if (provider is CultureInfo)
                    {
                        result = typeConverter.ConvertFrom(null, (CultureInfo)provider, s);
                    }
                    else
                    {
                        result = typeConverter.ConvertFrom(s);
                    }

                    return result;
                }
            }
            catch
            {
                if (returnDbNUllIfNotValid)
#if EJ2_DNX
                    return Convert.DBNull;
#else
                    return System.DBNull.Value;
#endif

                throw;
            }

            // throw new InvalidCastException(SR.GetString("InvalidCast_IConvertible"));
#if EJ2_DNX
            return Convert.DBNull;
#else
            return System.DBNull.Value;
#endif
        }

        /// <summary>
        /// Generates display text using the specified format, culture info and number format.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="valueType">The value type on which formatting is based. The original value will first be converted to this type.</param>
        /// <param name="format">The format like in ToString(string format).</param>
        /// <param name="ci">The <see cref="CultureInfo"/> for formatting the value.</param>
        /// <param name="nfi">The <see cref="NumberFormatInfo"/> for formatting the value.</param>
        /// <returns>The string with the formatted text for the value.</returns>
        public static string FormatValue(object value, Type valueType, string format, CultureInfo ci,
                                         NumberFormatInfo nfi)
        {
            string strResult;
            object obj;
            try
            {
                if (value is string)
                {
                    return (string)value;
                }
                else if (value is byte[]) // Picture
                {
                    return string.Empty;
                }
                else if (value == null || valueType == null || value.GetType() == valueType)
                {
                    obj = value;
                }
                else
                {
                    try
                    {
                        obj = ValueConvert.ChangeType(value, valueType, ci, true);
                    }
                    catch (Exception ex)
                    {
                        obj = value;
                        if (!(ex is FormatException || ex.InnerException is FormatException))
                        {
                            throw;
                        }
                    }
                }

                if (obj == null || obj is System.DBNull)
                {
                    strResult = string.Empty; // or "NullString"
                }
                else
                {
                    if (obj is IFormattable)
                    {
                        IFormattable formattableValue = (IFormattable)obj;
                        IFormatProvider provider = null;
                        if (nfi != null && !(obj is DateTime))
                        {
                            provider = nfi;
                        }
                        else if (ci != null)
                        {
                            provider = obj is DateTime
                                           ? (IFormatProvider)ci.DateTimeFormat
                                           : (IFormatProvider)ci.NumberFormat;
                        }

                        if (format != null && format.Length > 0 || nfi != null)
                        {
                            strResult = formattableValue.ToString(format, provider);
                        }
                        else
                        {
                            strResult = formattableValue.ToString();
                        }
                    }
                    else
                    {
                        TypeConverter tc = TypeDescriptor.GetConverter(obj.GetType());
                        if (tc.CanConvertTo(typeof(string)))
                        {
                            strResult = (string)tc.ConvertTo(null, ci, obj, typeof(string));
                        }
                        else if (obj is IConvertible)
                        {
                            strResult = Convert.ToString(obj, ci);
                        }
                        else
                        {
                            strResult = obj.ToString();
                        }
                    }
                }
            }
            catch
            {
                strResult = string.Empty;
                throw; // TODO: should I throw a more specific instead?
            }

            if (strResult == null)
            {
                strResult = string.Empty;
            }

            if (allowFormatValueTrimEnd)
            {
                strResult = strResult.TrimEnd();
            }

            return strResult;
        }

        private static bool allowFormatValueTrimEnd;

        /// <summary>
        /// Indicates whether <see cref="FormatValue"/> should trim whitespace characters from
        /// the end of the formatted text.
        /// </summary>
        public static bool AllowFormatValueTrimEnd
        {
            get { return allowFormatValueTrimEnd; }
            set { allowFormatValueTrimEnd = value; }
        }

        /// <summary>
        /// Returns a representative value for any given type.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>A value with the specified type.</returns>
        public static object GetDefaultValue(Type type)
        {
            object value;

            if (type == null)
            {
                return "0";
            }

            lock (cachedDefaultValues)
            {
                if (cachedDefaultValues.ContainsKey(type))
                {
                    value = cachedDefaultValues[type];
                }
                else
                {
                    switch (type.FullName)
                    {
                        case "System.Double":
                        case "System.Single":
                        case "System.Decimal":
                            value = 123.4567;
                            break;

                        case "System.Boolean":
                            value = true;
                            break;

                        case "System.String":
                            value = string.Empty;
                            break;

                        case "System.DateTime":
                            value = DateTime.Now;
                            break;

                        case "System.Int32":
                        case "System.Int16":
                        case "System.Int64":
                        case "System.SByte":
                        case "System.Byte":
                        case "System.UInt16":
                        case "System.UInt32":
                        case "System.UInt64":
                            value = 123;
                            break;

                        case "System.Char":
                            value = 'A';
                            break;
                        case "System.DBNull":
#if EJ2_DNX
                            value = Convert.DBNull;
#else
                            value = System.DBNull.Value;
#endif
                            break;
                        default:
                            value = string.Empty;
                            break;
                    }

                    cachedDefaultValues[type] = value;
                }

                return value;
            }
        }

        /// <summary>
        /// Overloaded. Parses the given string including type information. String can be in format %lt;type&gt; 'value'.
        /// </summary>
        /// <param name="valueAsString"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        private static bool ParseValueWithTypeInformation(string valueAsString, out object retVal)
        {
            return ParseValueWithTypeInformation(valueAsString, out retVal);
        }

        /// <summary>
        /// Parses the given string including type information. String can be in format %lt;type&gt; 'value'.
        /// </summary>
        /// <param name="valueAsString"></param>
        /// <param name="retVal"></param>
        /// <param name="allowConvertFromBase64">Indicates whether TypeConverter should be checked whether the type to be
        /// parsed supports conversion to/from byte array (e.g. an Image).</param>
        /// <returns></returns>
        public static bool ParseValueWithTypeInformation(string valueAsString, object retVal,
                                                         bool allowConvertFromBase64)
        {
            retVal = null;
            if (string.IsNullOrEmpty(valueAsString)) { valueAsString = string.Empty; }
            if (valueAsString.StartsWith("'", StringComparison.InvariantCulture) && valueAsString.EndsWith("'", StringComparison.InvariantCulture))
            {
                retVal = valueAsString.Substring(1, valueAsString.Length - 2);
                return true;
            }
            else if (valueAsString.StartsWith("<", StringComparison.InvariantCulture))
            {
                int closeBracket = valueAsString.IndexOf(">", StringComparison.InvariantCulture);
                if (closeBracket > 1)
                {
                    string typeName = valueAsString.Substring(1, closeBracket - 1);
                    if (typeName == "null")
                    {
                        retVal = null;
                        return true;
                    }
                    else if (typeName == "System.DBNull")
                    {
#if EJ2_DNX
                        retVal = Convert.DBNull;
#else
                        retVal = System.DBNull.Value;
#endif
                    }
                    else
                    {
                        valueAsString = valueAsString.Substring(closeBracket + 1).Trim();
                        if (valueAsString.StartsWith("'", StringComparison.InvariantCulture) && valueAsString.EndsWith("'", StringComparison.InvariantCulture))
                        {
                            valueAsString = valueAsString.Substring(1, valueAsString.Length - 2);
                            Type type = ValueConvert.GetType(typeName);
                            if (type != null)
                            {
                                bool handled = false;

                                if (allowConvertFromBase64)
                                {
                                    handled = TryConvertFromBase64String(type, valueAsString, out retVal);
                                }

                                if (!handled)
                                {
                                    retVal = ValueConvert.Parse(valueAsString, type,
                                                                System.Globalization.CultureInfo.InvariantCulture, string.Empty);
                                }

                                return true;
                            }
                        }
                    }
                }
            }

            retVal = valueAsString;
            return false;
        }

        /// <summary>
        /// Indicates whether the TypeConverter associated with the type supports conversion to/from a byte array (e.g. an Image).
        /// If that is the case the string is converted to a byte array from a base64 string.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="valueAsString"></param>
        /// <param name="retVal"></param>
        /// <returns></returns>
        public static bool TryConvertFromBase64String(Type type, string valueAsString, out object retVal)
        {
            bool handled = false;
            retVal = null;
            TypeConverter tc = TypeDescriptor.GetConverter(type);
            if (tc != null)
            {
                // e.g. an Image
                if (tc.CanConvertFrom(typeof(byte[])))
                {
                    byte[] byteArray = (byte[])Convert.FromBase64String(valueAsString);
                    retVal = tc.ConvertFrom(byteArray);
                    handled = true;
                }
                else if (tc.CanConvertFrom(typeof(MemoryStream)))
                {
                    using MemoryStream ms = new MemoryStream((byte[])Convert.FromBase64String(valueAsString));
                    retVal = tc.ConvertFrom(ms);
                    handled = true;
                }
            }

            return handled;
        }

        /// <summary>
        /// Overloaded. Formats the given value as string including type information. String will be in format %lt;type&gt; 'value'.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string FormatValueWithTypeInformation(object value)
        {
            return FormatValueWithTypeInformation(value, false);
        }

        /// <summary>
        /// Formats the given value as string including type information. String will be in format %lt;type&gt; 'value'.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="allowConvertToBase64">Indicates whether TypeConverter should be checked whether the type to be
        /// parsed supports conversion to/from byte array (e.g. an Image).</param>
        /// <returns></returns>
        public static string FormatValueWithTypeInformation(object value, bool allowConvertToBase64)
        {
            allowConvertToBase64 = false;
            if (value is string)
            {
                return "'" + (string)value + "'";
            }
            else if (value is DBNull)
            {
                return "<System.DBNull>";
            }
            else if (value == null)
            {
                return "<null>";
            }
            else
            {
                string valueAsString = null;
#if EJ2_DNX
                if (allowConvertToBase64)
                    valueAsString = TryConvertToBase64String(value);
#endif
                valueAsString = ValueConvert.FormatValue(value, typeof(string), "",
                                                             System.Globalization.CultureInfo.InvariantCulture, null);

                return "<" + GetTypeName(value.GetType()) + "> '" + valueAsString + "'";
            }
        }

#if EJ2_DNX
        /// <summary>
        /// Indicates whether the TypeConverter associated with the type supports conversion to/from a byte array (e.g. an Image). 
        /// If that is the case the string is converted to a base64 string from a byte array.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string TryConvertToBase64String(object value)
        {
            string valueAsString = null;
            TypeConverter tc = TypeDescriptor.GetConverter(value);
            if (tc != null)
            {
                // e.g. an Image
                if (tc.CanConvertTo(typeof(byte[])))
                {
                    byte[] byteArray = (byte[])tc.ConvertTo(value, typeof(byte[]));
                    valueAsString = Convert.ToBase64String(byteArray);
                }
                else if (tc.CanConvertTo(typeof(MemoryStream)))
                {
                    MemoryStream ms = (MemoryStream)tc.ConvertTo(value, typeof(MemoryStream));
                    valueAsString = Convert.ToBase64String(ms.ToArray());
                }
            }
            return valueAsString;
        }
#endif

        /// <summary>
        /// Returns the type name. If type is not in mscorlib, the assembly name is appended.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeName(Type type)
        {
#if EJ2_DNX
            if (!type.IsPrimitive && type.Module != typeof(object).Module)
                return type.FullName + ", " + System.IO.Path.GetFileNameWithoutExtension(type.Module.ScopeName);
#endif
            return type?.FullName;
        }

        /// <summary>
        /// Returns the type from the specified name. If an assembly name is appended the list of currently loaded
        /// assemblies in the current AppDomain are checked.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetType(string typeName)
        {
#if EJ2_DNX
            string[] parts = typeName.Split(',');
            if (parts.Length == 2)
            {
                // Module name without version information.
                ResolveEventArgs e = new ResolveEventArgs(parts[1].Trim());
                Assembly assembly = AssemblyResolver(null, e);
                if (assembly != null)
                    return assembly.GetType(parts[0]);
            }
#endif
            return Type.GetType(typeName);
        }

#if EJ2_DNX
        public static Assembly AssemblyResolver(object sender, System.ResolveEventArgs e)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int n = 0; n < assemblies.Length; n++)
            {
                if (assemblies[n].GetName().Name == e.Name)
                    return assemblies[n];
            }
            return null;
        }
#endif

        /// <summary>
        /// Indicates whether string is null or empty.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsEmpty(string str)
        {
            return str == null || str.Length == 0;
        }
    }
}