using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.Serialization;
using System.Dynamic;
using System.Text.Json.Serialization;

namespace ASPNetCoreGraphQlServer.Data
{
    /// <summary>
    /// Specifies the FilterType to be used in LINQ methods.
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Performs LessThan operation.
        /// </summary>
        LessThan,

        /// <summary>
        /// Performs LessThan Or Equal operation.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Checks Equals on the operands.
        /// </summary>
        Equals,

        /// <summary>
        /// Checks for Not Equals on the operands.
        /// </summary>
        NotEquals,

        /// <summary>
        /// Checks for Greater Than or Equal on the operands.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Checks for Greater Than on the operands.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Checks for StartsWith on the string operands.
        /// </summary>
        StartsWith,

        /// <summary>
        /// Checks for EndsWith on the string operands.
        /// </summary>
        EndsWith,

        /// <summary>
        /// Checks for Contains on the string operands.
        /// </summary>
        Contains,

        /// <summary>
        /// Returns invalid type
        /// </summary>
        Undefined,

        /// <summary>
        /// Checks for Between two date on the operands.
        /// </summary>
        Between
    }

    /// <summary>
    /// Specifies the Filter Behaviour for the filter predicates.
    /// </summary>
    public enum FilterBehavior
    {
        /// <summary>
        /// Parses only StronglyTyped values.
        /// </summary>
        StronglyTyped,

        /// <summary>
        /// Parses all values by converting them as string.
        /// </summary>
        StringTyped
    }

    /// <summary>
    /// Specifies the Filter Behaviour for the filter predicates.
    /// </summary>
    public enum ColumnFilter
    {
        /// <summary>
        /// Parses only StronglyTyped values.
        /// </summary>
        Value,

        /// <summary>
        /// Parses all values by converting them as string.
        /// </summary>
        DisplayText
    }

    /// <summary>
    /// Defines the sort column.
    /// </summary>
    public class SortColumn
    {
        public SortColumn()
        {
            SortDirection = ListSortDirection.Ascending;
        }

        /// <summary>
        /// Specifies the column name.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Specifies the sort direction.
        /// </summary>
        public ListSortDirection SortDirection { get; set; }
    }

    internal class EnumerationValue
    {
        internal static object GetValueFromEnumMember(string description, Type EnumType)
        {
            var type = EnumType;
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                type = underlyingType;
            }
            if (!type.IsEnum)
            {
                throw new InvalidOperationException();
            }

            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(
                    field,
                    typeof(EnumMemberAttribute)) as EnumMemberAttribute;
                if (attribute != null)
                {
                    if (attribute.Value == description)
                    {
                        return field.GetValue(null);
                    }
                }
                else
                {
                    if (field.Name == description)
                    {
                        return field.GetValue(null);
                    }
                }
            }

            return null;
        }
    }

    public enum ListSortDirection
    {
        /// <summary>
        /// Sorts in ascending order.
        /// </summary>
        Ascending = 0,

        /// <summary>
        /// Sorts in descending order.
        /// </summary>
        Descending = 1,
    }

    /// <summary>
    /// Sepcifies the sort order.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// No sort order.
        /// </summary>
        [EnumMember(Value = "None")]
        None,

        /// <summary>
        /// Sorts in ascending order.
        /// </summary>
        [EnumMember(Value = "Ascending")]
        Ascending,

        /// <summary>
        /// Sorts in descending order.
        /// </summary>
        [EnumMember(Value = "Descending")]
        Descending,
    }

    /// <summary>
    /// Defines the sort column.
    /// </summary>
    public class SortedColumn
    {
        private SortOrder direction = SortOrder.Ascending;

        /// <summary>
        /// Specifies the field to sort.
        /// </summary>
        [JsonPropertyName("field")]
        [DefaultValue(null)]
        public string Field { get; set; }

        /// <summary>
        /// Specifies the sort order.
        /// </summary>
        [JsonPropertyName("direction")]
        [DefaultValue(SortOrder.Ascending)]
        [JsonConverter(typeof(JsonStringEnumConverter))]

        public SortOrder Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        /// <summary>
        /// Gets the sort comparer
        /// </summary>
        public object Comparer { get; set; }
    }

    public struct SortDescription : IEquatable<SortDescription>
    {
        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.SortDescription structure.
        /// </summary>
        /// <param name="propertyName">The name of the property to sort the list by.</param>
        /// <param name="direction">The sort order.</param>
        public SortDescription(string propertyName, ListSortDirection direction)
        {
            this.direction = direction;
            this.propertyName = propertyName;
        }

        /// <summary>
        /// Compares two System.ComponentModel.SortDescription objects for value inequality.
        /// </summary>
        /// <param name="sd1">The first instance to compare.</param>
        /// <param name="sd2">The second instance to compare.</param>
        /// <returns>bool.</returns>
        public static bool operator !=(SortDescription sd1, SortDescription sd2)
        {
            return !sd1.Equals(sd2);
        }

        /// <summary>
        /// Compares two System.ComponentModel.SortDescription objects for value equality.
        /// </summary>
        /// <param name="sd1">The first instance to compare.</param>
        /// <param name="sd2">The second instance to compare.</param>
        /// <returns>true.</returns>
        public static bool operator ==(SortDescription sd1, SortDescription sd2)
        {
            return sd1.Equals(sd2);
        }

        private ListSortDirection direction;

        /// <summary>
        /// Gets or sets a value that indicates whether to sort in ascending or descending
        ///     order.
        /// </summary>
        public ListSortDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        private string propertyName;

        /// <summary>
        /// Gets or sets the property name being used as the sorting criteria.
        /// </summary>
        public string PropertyName
        {
            get { return propertyName; }
            set { propertyName = value; }
        }

        /// <summary>
        /// Compares the specified instance and the current instance of System.ComponentModel.SortDescription
        ///     for value equality.
        /// </summary>
        /// <param name="obj">The System.ComponentModel.SortDescription instance to compare.</param>
        /// <returns>true.</returns>
        public override bool Equals(object obj)
        {
            return true;
        }
        /// <summary>
        /// Compares the specified instance and the current instance of System.ComponentModel.SortDescription
        ///     for value equality.
        /// </summary>
        /// <param name="other">The System.ComponentModel.SortDescription instance to compare.</param>
        /// <returns>true.</returns>
        public bool Equals(SortDescription other) => true;

        /// <summary>
        /// Returns the hash code.
        /// </summary>
        /// <returns>int.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}