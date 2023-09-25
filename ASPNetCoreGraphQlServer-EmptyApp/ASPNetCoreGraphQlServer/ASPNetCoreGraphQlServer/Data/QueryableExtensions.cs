namespace ASPNetCoreGraphQlServer.Data
{
    using System;
    using System.Xml;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.ComponentModel;
    using System.Globalization;
    using System.Collections.ObjectModel;
    using System.Dynamic;
    using System.Reflection.Emit;
    using ASPNetCoreGraphQlServer.Models;
    using ASPNetCoreGraphQlServer.Data;
    using ASPNetCoreGraphQlServer.GraphQl;
#if EJ2_DNX
    using System.Data;
#endif

    /// <summary>
    /// Provides extension methods for Queryable source.
    /// <para></para>
    /// <para></para>
    /// <para>var fonts = FontFamily.Families.AsQueryable();. </para>
    /// <para></para>
    /// <para></para>
    /// <para>We would normally write Expressions as,. </para>
    /// <para></para>
    /// <code lang="C#">var names = new string[] {&quot;Tony&quot;, &quot;Al&quot;,
    /// &quot;Sean&quot;, &quot;Elia&quot;}.AsQueryable();
    /// names.OrderBy(n=&gt;n);</code>
    /// <para></para>
    /// <para></para>
    /// <para>This would sort the names based on alphabetical order. Like so, the
    /// Queryable extensions are a set of extension methods that define functions which
    /// will generate expressions based on the supplied values to the functions.</para>
    /// </summary>
    /// <exclude/>
    public static class QueryableExtensions
    {
        private static readonly Type[] EmptyTypes = Type.EmptyTypes;

        public static IEnumerable OfQueryable(this IEnumerable items)
        {
            var enumerator = items?.GetEnumerator();
            if (enumerator != null && enumerator.MoveNext())
            {
                if (enumerator.Current != null)
                {
                    var type = enumerator.Current.GetType();
                    IQueryable queryable = items.AsQueryable();
                    return queryable.OfType(type);
                }
            }

            return items;
        }

        public static IEnumerable OfQueryable(this IEnumerable items, Type sourceType)
        {
            IQueryable queryable = items.AsQueryable();
            return queryable.OfType(sourceType);
        }

        /// <summary>
        /// Generates an AND binary expression for the given Binary expressions.
        /// <para></para>
        /// </summary>
        /// <param name="expr1"></param>
        /// <param name="expr2"></param>
        public static BinaryExpression AndPredicate(this Expression expr1, Expression expr2)
        {
            return Expression.And(expr1, expr2);
        }

        public static BinaryExpression AndAlsoPredicate(this Expression expr1, Expression expr2)
        {
            return Expression.AndAlso(expr1, expr2);
        }

        public static BinaryExpression OrElsePredicate(this Expression expr1, Expression expr2)
        {
            return Expression.OrElse(expr1, expr2);
        }

        public static int Count(this IQueryable source)
        {
            var sourceType = source?.ElementType;
            return (Int32)source?.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "Count",
                    new Type[] { sourceType },
                    new Expression[] { source.Expression }));
        }

        public static object ElementAt(this IQueryable source, int index, Type sourceType)
        {
            return source?.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "ElementAt",
                    new Type[] { sourceType },
                    new Expression[] { source.Expression, Expression.Constant(index) }));
        }

        public static object ElementAt(this IQueryable source, int index)
        {
            var sourceType = source?.ElementType;
            return source?.ElementAt(index, sourceType);
        }

        public static object ElementAtOrDefault(this IQueryable source, int index, Type sourceType)
        {
            return source?.Provider.Execute(
                Expression.Call(
                    typeof(Queryable),
                    "ElementAtOrDefault",
                    new Type[] { sourceType },
                    new Expression[] { source.Expression, Expression.Constant(index) }));
        }

        public static object ElementAtOrDefault(this IQueryable source, int index)
        {
            var sourceType = source?.ElementType;
            return source?.ElementAtOrDefault(index, sourceType);
        }

#if EJ2_DNX
        public static IQueryable GroupBy(this IQueryable source, IEnumerable<SortColumn> groupByNames, Type sourceType)
        {
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            var selector = GenerateNew(groupByNames.Select(s => s.ColumnName).ToList(), paramExpression);
            var lambda = Expression.Lambda(selector, paramExpression);
            var method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "GroupBy" && m.GetParameters().Length == 2);
            var methodCallExp = Expression.Call(null, method.MakeGenericMethod(new
                                                                                   Type[] {source.ElementType, lambda.Body.Type}),
                                                new Expression[] {source.Expression, lambda});
            var groupedSource = source.Provider.CreateQuery(methodCallExp);

            //.OrderBy()
            var sortParamExpression = Expression.Parameter(groupedSource.ElementType, "o");
            var mExp = Expression.PropertyOrField(sortParamExpression, "Key");
            groupByNames.IterateIndex<SortColumn>((i, s) =>
                {
                    if (s.SortDirection == ListSortDirection.Ascending)
                    {
                        if (i == 0)
                        {
                            groupedSource = groupedSource.OrderBy(sortParamExpression, Expression.PropertyOrField(mExp, s.ColumnName));
                        }
                        else
                        {
                            groupedSource = groupedSource.ThenBy(sortParamExpression, Expression.PropertyOrField(mExp, s.ColumnName));
                        }
                    }
                    else
                    {
                        if (i == 0)
                        {
                            groupedSource = groupedSource.OrderByDescending(sortParamExpression, Expression.PropertyOrField(mExp,s.ColumnName));
                        }
                        else
                        {
                            groupedSource = groupedSource.ThenByDescending(sortParamExpression, Expression.PropertyOrField(mExp, s.ColumnName));
                        }
                    }
                });

            //.Select()
            var groupparamExpression = Expression.Parameter(groupedSource.ElementType, "g");
            var nExp = Expression.New(typeof (GroupContext));
            var bindings = new List<MemberBinding>();
            //bindings.Add(Expression.Bind(typeof(GroupContext).GetMember("Key")[0], Expression.PropertyOrField(groupparamExpression, "Key")));
            bindings.Add(Expression.Bind(typeof (GroupContext).GetMember("Details").FirstOrDefault(),
                                         groupparamExpression));
            Expression e = Expression.MemberInit(nExp, bindings.ToArray());
            //g=>prop.Propertyname
            var selectLambda = Expression.Lambda(e, groupparamExpression);
            return groupedSource.Provider.CreateQuery(Expression.Call(
                typeof (Queryable), "Select",
                new Type[] {groupedSource.ElementType, typeof (GroupContext)},
                new Expression[] {groupedSource.Expression, selectLambda}));
        }

        public static IQueryable GroupBy(this IQueryable source, IEnumerable<SortColumn> groupByNames)
        {
            var sourceType = source.ElementType;
            return source.GroupBy(groupByNames, sourceType);
        }

        /// <summary>
        /// Generates the GroupBy Expression
        /// </summary>
        /// <param name="groupByName"></param>
        /// <param name="sortAction"></param>
        /// <param name="source"></param>
        /// <param name="sourceType"></param>
        public static IQueryable GroupBy(this IQueryable source, string groupByName, string sortAction, Type sourceType)
        {
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // code for convert complex property to simple property
            var propertyName = groupByName;
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var lambda = Expression.Lambda(memExp, paramExpression);
            //return this.Source.Provider.CreateQuery(
            //.GroupBy()
            var groupedSource = source.Provider.CreateQuery(Expression.Call(
                typeof (Queryable), "GroupBy",
                new Type[]
                    {
                        source.ElementType,
                        sourceType.GetProperty(groupByName).PropertyType
                    },
                source.Expression,
                Expression.Quote(lambda)));

            //.OrderBy()
            ParameterExpression sortParamExpression = Expression.Parameter(groupedSource.ElementType, "o");
            // Code Make complex property to simple
            var mExp = Expression.PropertyOrField(sortParamExpression, "Key");
            var sortLambda = Expression.Lambda(mExp, sortParamExpression);
            var orderedSource = groupedSource.Provider.CreateQuery(
                Expression.Call(
                    typeof (Queryable),
                    sortAction,
                    new Type[] {groupedSource.ElementType, sortLambda.Body.Type},
                    groupedSource.Expression,
                    sortLambda));

            //.Select()
            var groupparamExpression = Expression.Parameter(orderedSource.ElementType, "g");
            var bindings = new List<MemberBinding>();
            //bindings.Add(Expression.Bind(typeof(GroupContext).GetMember("Key")[0], Expression.PropertyOrField(groupparamExpression, "Key")));
            bindings.Add(Expression.Bind(typeof (GroupContext).GetMember("Details").FirstOrDefault(),
                                         groupparamExpression));
            Expression e = Expression.MemberInit(Expression.New(typeof (GroupContext)), bindings);
            //g=>prop.Propertyname
            var selectLambda = Expression.Lambda(e, groupparamExpression);
            var result = orderedSource.Provider.CreateQuery(Expression.Call(
                typeof (Queryable),
                "Select",
                new Type[] {orderedSource.ElementType, selectLambda.Body.Type},
                new Expression[] {orderedSource.Expression, selectLambda}));
            return result;
        }

        public static IQueryable GroupBy(this IQueryable source, string groupByName, string sortAction)
        {
            var sourceType = source.ElementType;
            return source.GroupBy(groupByName, sortAction, sourceType);
        }

#endif
        public static IQueryable OfType(this IQueryable source, Type sourceType)
        {
            return source?.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "OfType",
                    new Type[] { sourceType }, new Expression[] { source.Expression }));
        }

        /// <summary>
        /// Generates a OrderBy query for the Queryable source.
        /// <para></para>
        /// <code lang="C#">            DataClasses1DataContext db = new
        /// DataClasses1DataContext();
        ///             var orders = db.Orders.Skip(0).Take(10).ToList();
        ///             var queryable = orders.AsQueryable();
        ///             var sortedOrders =
        /// queryable.OrderBy(&quot;ShipCountry&quot;);</code>
        /// <para></para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType"></param>
        public static IQueryable OrderBy(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // Expression memExp = paramExpression.GetValueExpression(propertyName, source.ElementType);
            // LambdaExpression lambda = Expression.Lambda(memExp, paramExpression);
            var lambda = GetLambdaWithComplexPropertyNullCheck(source, propertyName ?? string.Empty, paramExpression, sourceType);

            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "OrderBy",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression, lambda));
        }

        /// <summary>
        /// Generates lambda expression for the complex properties.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="paramExpression"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        private static LambdaExpression GetLambdaWithComplexPropertyNullCheck(IEnumerable source, string propertyName,
                                                                              ParameterExpression paramExpression, Type sourceType)
        {
            LambdaExpression lambda = null; _ = source;
            var properties = propertyName.Split(new char[] { '.' });

            if (properties.GetLength(0) > 1)
            {
                // has complex properties... need to check each level for null & return null if any level is null...
                var memExp = paramExpression.GetValueExpression(propertyName, sourceType);

                // make memExp type object so it can be compared with null below
                if (memExp.Type != typeof(object))
                {
                    memExp = Expression.Convert(memExp, typeof(object));
                }

                Expression memExp2 = null;
                string name = string.Empty;
                int count = properties.GetLength(0);
                for (int i = 0; i < count; i++)
                {
                    if (i == 0) // the first one
                    {
                        memExp2 = Expression.Equal(
                            paramExpression.GetValueExpression(properties[i], sourceType), Expression.Constant(null));
                        name = properties[i];
                    }
                    else if (i < count - 1) // don't add the last inner property check as it will be added after this loop
                    {
                        name += '.' + properties[i];
                        memExp2 = Expression.OrElse(
                            memExp2,
                            Expression.Equal(paramExpression.GetValueExpression(name, sourceType), Expression.Constant(null)));
                    }
                }
#if SyncfusionFramework4_0
                memExp2 = Expression.Condition(memExp2, Expression.Constant(null), memExp, typeof(object));
#else
                memExp2 = Expression.Condition(memExp2, Expression.Constant(null), memExp);
#endif
                lambda = Expression.Lambda(memExp2, paramExpression);
            }
            else
            {
                Expression memExp = paramExpression.GetValueExpression(propertyName, sourceType);
                lambda = Expression.Lambda(memExp, paramExpression);
            }

            return lambda;
        }

        private static LambdaExpression GetLambdaWithComplexPropertyNullCheck(IQueryable source, string propertyName,
                                                                              ParameterExpression paramExpression, Type sourceType)
        {
            LambdaExpression lambda = null; _ = source;
            var properties = propertyName.Split(new char[] { '.' });

            if (properties.GetLength(0) > 1)
            {
                // has complex properties... need to check each level for null & return null if any level is null...
                var memExp = paramExpression.GetValueExpression(propertyName, sourceType);

                // make memExp type object so it can be compared with null below
                if (memExp.Type != typeof(object))
                {
                    memExp = Expression.Convert(memExp, typeof(object));
                }

                Expression memExp2 = null;
                string name = string.Empty;
                int count = properties.GetLength(0);
                for (int i = 0; i < count; i++)
                {
                    if (i == 0) // the first one
                    {
                        memExp2 = Expression.Equal(
                            paramExpression.GetValueExpression(properties[i], sourceType), Expression.Constant(null));
                        name = properties[i];
                    }
                    else if (i < count - 1) // don't add the last inner property check as it will be added after this loop
                    {
                        name += '.' + properties[i];
                        memExp2 = Expression.OrElse(
                            memExp2,
                            Expression.Equal(paramExpression.GetValueExpression(name, sourceType), Expression.Constant(null)));
                    }
                }
#if SyncfusionFramework4_0
                memExp2 = Expression.Condition(memExp2, Expression.Constant(null), memExp, typeof(object));
#else
                memExp2 = Expression.Condition(memExp2, Expression.Constant(null), memExp);
#endif
                lambda = Expression.Lambda(memExp2, paramExpression);
            }
            else
            {
                Expression memExp = paramExpression.GetValueExpression(propertyName, sourceType);
                lambda = Expression.Lambda(memExp, paramExpression);
            }

            return lambda;
        }

        public static IQueryable OrderBy(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.OrderBy(propertyName, sourceType);
        }

        public static IQueryable OrderBy(this IQueryable source, string propertyName,
                                         Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            return OrderBy(source, paramExpression, iExp);
        }

        public static IQueryable OrderBy(this IQueryable source, string propertyName, IComparer<object> comparer,
                                         Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            var lambda = Expression.Lambda(iExp, paramExpression);
            var method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 3);
            var conExp = Expression.Constant(comparer, typeof (IComparer<object>));
            var methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable OrderBy(this IQueryable source, ParameterExpression paramExpression, Expression mExp)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var lambda = Expression.Lambda(mExp, paramExpression);
            var orderedSource = source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "OrderBy",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
            return orderedSource;
        }

        /// <summary>
        /// Generates an OrderBy query for the IComparer defined.
        /// <para></para>
        /// <para> </para>
        /// <code lang="C#">   public class OrdersComparer :
        /// IComparer&lt;Order&gt;
        ///     {
        ///         public int Compare(Order x, Order y)
        ///         {
        ///             return string.Compare(x.ShipCountry, y.ShipCountry);
        ///         }
        ///     }</code>
        /// <para></para>
        /// <para><code lang="C#">var sortedOrders =
        /// db.Orders.Skip(0).Take(5).ToList().OrderBy(o =&gt; o, new
        /// OrdersComparer());</code></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="comparer"></param>
        /// <param name="sourceType"></param>
        public static IQueryable OrderBy<T>(this IQueryable source, IComparer<T> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof (IComparer<T>));
            MethodCallExpression methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable OrderBy<T>(this IQueryable source, IComparer<T> comparer)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.OrderBy<T>(comparer, sourceType);
        }

        public static IQueryable OrderBy(this IQueryable source, string propertyName, IComparer<object> comparer,
                                         Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); } _ = propertyName;
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderBy" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof (IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        /// <summary>
        /// Generates an OrderByDescending query for the IComparer defined.
        /// <para></para>
        /// <para> </para>
        /// <code lang="C#">   public class OrdersComparer :
        /// IComparer&lt;Order&gt;
        ///     {
        ///         public int Compare(Order x, Order y)
        ///         {
        ///             return string.Compare(x.ShipCountry, y.ShipCountry);
        ///         }
        ///     }</code>
        /// <para></para>
        /// <para><code lang="C#">var sortedOrders =
        /// db.Orders.Skip(0).Take(5).ToList().OrderByDescending(o =&gt; o, new
        /// OrdersComparer());</code></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="comparer"></param>
        /// <param name="sourceType"></param>
        public static IQueryable OrderByDescending<T>(this IQueryable source, IComparer<T> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof (IComparer<T>));
            MethodCallExpression methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable OrderByDescending(this IQueryable source, string propertyName,
                                                   IComparer<object> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); } _ = propertyName;
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof (IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable OrderByDescending<T>(this IQueryable source, IComparer<T> comparer)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.OrderByDescending<T>(comparer, sourceType);
        }

        /// <summary>
        /// Generates a OrderByDescending query for the Queryable source.
        /// <para></para>
        /// <code lang="C#">            DataClasses1DataContext db = new
        /// DataClasses1DataContext();
        ///             var orders = db.Orders.Skip(0).Take(10).ToList();
        ///             var queryable = orders.AsQueryable();
        ///             var sortedOrders =
        /// queryable.OrderByDescending(&quot;ShipCountry&quot;);</code>
        /// <para></para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType"></param>
        public static IQueryable OrderByDescending(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // Expression memExp = paramExpression.GetValueExpression(propertyName, source.ElementType);
            // LambdaExpression lambda = Expression.Lambda(memExp, paramExpression);
            LambdaExpression lambda = GetLambdaWithComplexPropertyNullCheck(source, propertyName ?? string.Empty, paramExpression, sourceType);

            // Previously  source.ElementType passed as parameter. This will leads to conflict when we use different classes derived from one interface. Now passing sourType as parameter to resolve this issue.
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "OrderByDescending",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
        }

        public static Expression GetExpression(this ParameterExpression paramExpression, string propertyName)
        {
            return paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
        }

        /// <summary>
        /// Generate expression from simple and complex property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="sourceType"></param>
        /// <param name="paramExpression"></param>
        /// <returns></returns>
        public static Expression GetValueExpression(this ParameterExpression paramExpression, string propertyName,
                                                    Type sourceType)
        {
            Expression exp = null;
            bool isExpando = false;
            bool isDynamic = false;
#if EJ2_DNX
            if (typeof(ICustomTypeDescriptor).IsAssignableFrom(sourceType))
            {
                //covert expression to ICustomTypeDescriptor
                Expression exp1 = System.Linq.Expressions.Expression.Convert(paramExpression, typeof(ICustomTypeDescriptor));
                exp = (System.Linq.Expressions.Expression<Func<ICustomTypeDescriptor, object, object>>)((t, o) => t.GetProperties()[propertyName].GetValue(o));
                //return the value as an expression.
                exp = System.Linq.Expressions.Expression.Invoke(exp, new Expression[] { exp1, paramExpression });
                return exp;
            }
            else
            {
#endif
                // Split the complex property to simple property and generate member expression
            string[] propertyNameList = propertyName?.Split('.') ?? Array.Empty<string>();
            foreach (string property in propertyNameList)
            {
                if (exp != null)
                {
                    if (string.Equals(nameof(ExpandoObject), exp.Type.Name, StringComparison.Ordinal) || isExpando)
                    {
                        // handle Expando object
                        Expression param = Expression.Convert(exp, typeof(IDictionary<string, object>));
                        exp = Expression.Property(param, "Item", new Expression[] { Expression.Constant(property) });
                        isExpando = true;
                    }
                    else if (exp.Type.IsSubclassOf(typeof(DynamicObject)) || isDynamic)
                    {
                        //// handle Dynamic object
                        //Expression param = Expression.Convert(exp, typeof(DynamicObject));
                        //MethodInfo methodName = typeof(DataUtil).GetMethod(nameof(DataUtil.GetDynamicValue));
                        //exp = Expression.Call(methodName, param, Expression.Constant(property));
                        //isDynamic = true;
                    }
                    else if (!isDynamic && !isExpando)
                    {
                        exp = Expression.PropertyOrField(exp, property);
                    }
                    }
                    else
                    {
                        exp = paramExpression;
                        if (paramExpression?.Type != sourceType)
                    {
                        exp = Expression.Convert(paramExpression, sourceType);
                    }

                    exp = Expression.PropertyOrField(exp, property);
                }
            }
#if EJ2_DNX
            }
#endif
            return exp;
        }

        public static IQueryable OrderByDescending(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.OrderByDescending(propertyName, sourceType);
        }

        public static IQueryable OrderByDescending(this IQueryable source, string propertyName,
                                                   IComparer<object> comparer,
                                                   Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            LambdaExpression lambda = Expression.Lambda(iExp, paramExpression);
            MethodInfo method =
                typeof (Queryable).GetMethods().FirstOrDefault(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof (IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(null,
                                                             method.MakeGenericMethod(new Type[]
                                                                 {
                                                                    source.ElementType, lambda.Body.Type
                                                                 }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable OrderByDescending(this IQueryable source, string propertyName,
                                                   Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            return OrderByDescending(source, paramExpression, iExp);
        }

        public static IQueryable OrderByDescending(this IQueryable source, ParameterExpression paramExpression,
                                                   Expression mExp)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            LambdaExpression lambda = Expression.Lambda(mExp, paramExpression);
            var orderedSource = source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "OrderByDescending",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
            return orderedSource;
        }

        /// <summary>
        /// Generates an OR binary expression for the given Binary expressions.
        /// <para></para>
        /// </summary>
        /// <param name="expr1"></param>
        /// <param name="expr2"></param>
        public static BinaryExpression OrPredicate(this Expression expr1, Expression expr2)
        {
            return Expression.Or(expr1, expr2);
        }

        /// <summary>
        /// Creates a ParameterExpression that is required when building a series of
        /// predicates for the WHERE filter.
        /// <para></para>
        /// <code lang="C#">        DataClasses1DataContext db = new
        /// DataClasses1DataContext();
        ///         var orders = db.Orders.Skip(0).Take(100).ToList();
        ///         var queryable = orders.AsQueryable();
        ///         var parameter =
        /// queryable.Parameter();</code>
        /// <para></para>
        /// <para></para>Use this same parameter passed to generate different predicates and
        /// finally to generate the Lambda.
        /// </summary>
        /// <remarks>
        /// If we specify a parameter for every predicate, then the Lambda expression scope
        /// will be out of the WHERE query that gets generated.
        /// </remarks>
        /// <param name="source"></param>
        public static ParameterExpression Parameter(this IQueryable source)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            ParameterExpression paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            return paramExpression;
        }

        public static ParameterExpression Parameter(this Type sourceType)
        {
            return Expression.Parameter(sourceType, sourceType?.Name);
        }

        public static Expression Equal(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.Equal(memExp,
                                                     System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression Equal(this ParameterExpression paramExpression, string propertyName,
                                             string propertyName2)
        {
            var memExp = paramExpression.GetExpression(propertyName); _ = propertyName2;
            var memExp2 = paramExpression.GetExpression(propertyName);
            BinaryExpression bExp = Expression.Equal(memExp, memExp2);
            return bExp;
        }

        public static Expression Equal(this ParameterExpression paramExpression, string propertyName, object value,
                                       Type elementType, Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return typed value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc});
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.Equal(invokeExp, rightOperandExpression);
            return bExp;
        }

        public static Expression NotEqual(this ParameterExpression paramExpression, string propertyName, object value,
                                          Type elementType, Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return typed value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc });
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.NotEqual(invokeExp, rightOperandExpression);
            return bExp;
        }

        public static BinaryExpression NotEqual(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetExpression(propertyName);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.NotEqual(memExp,
                                                        System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression NotEqual(this ParameterExpression paramExpression, string propertyName, string propertyName2)
        {
            var memExp = paramExpression.GetExpression(propertyName);
            var memExp2 = paramExpression.GetExpression(propertyName2);
            BinaryExpression bExp = Expression.NotEqual(memExp, memExp2);
            return bExp;
        }

        public static BinaryExpression GreaterThanOrEqual(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.GreaterThanOrEqual(memExp,
                                                                  System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression GreaterThanOrEqual(this ParameterExpression paramExpression, string propertyName, string propertyName2)
        {
            var memExp = paramExpression.GetExpression(propertyName);
            var memExp2 = paramExpression.GetExpression(propertyName2);
            BinaryExpression bExp = Expression.GreaterThanOrEqual(memExp, memExp2);
            return bExp;
        }

        public static Expression GreaterThanOrEqual(this ParameterExpression paramExpression, string propertyName,
                                                    object value, Type elementType, Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return Int32 value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc });
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.GreaterThanOrEqual(invokeExp, rightOperandExpression);
            return bExp;
        }

        public static BinaryExpression GreaterThan(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.GreaterThan(memExp,
                                                           System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression GreaterThan(this ParameterExpression paramExpression, string propertyName, string propertyName2)
        {
            var memExp = paramExpression.GetExpression(propertyName);
            var memExp2 = paramExpression.GetExpression(propertyName2);
            BinaryExpression bExp = Expression.GreaterThan(memExp, memExp2);
            return bExp;
        }

        public static Expression GreaterThan(this ParameterExpression paramExpression, string propertyName, object value,
                                             Type elementType, Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return typed value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc });
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.GreaterThan(invokeExp, rightOperandExpression);
            return bExp;
        }

        public static BinaryExpression LessThan(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.LessThan(memExp,
                                                        System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression LessThan(this ParameterExpression paramExpression, string propertyName, string propertyName2)
        {
            var memExp = paramExpression.GetExpression(propertyName);
            var memExp2 = paramExpression.GetExpression(propertyName2);
            BinaryExpression bExp = Expression.LessThan(memExp, memExp2);
            return bExp;
        }

        public static Expression LessThan(this ParameterExpression paramExpression, string propertyName, object value,
                                          Type elementType, Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return Int32 value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc });
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.LessThan(invokeExp, rightOperandExpression);
            return bExp;
        }

        public static BinaryExpression LessThanOrEqual(this ParameterExpression paramExpression, string propertyName, object value)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var result = NullableHelperInternal.FixDbNUllasNull(value, memExp.Type);
            result = NullableHelperInternal.ChangeType(result, memExp.Type, CultureInfo.InvariantCulture);
            BinaryExpression bExp = Expression.LessThanOrEqual(memExp,
                                                               System.Linq.Expressions.Expression.Constant(result, memExp.Type));
            return bExp;
        }

        public static BinaryExpression LessThanOrEqual(this ParameterExpression paramExpression, string propertyName, string propertyName2)
        {
            var memExp = paramExpression.GetValueExpression(propertyName, paramExpression?.Type);
            var memExp2 = paramExpression.GetExpression(propertyName2);
            BinaryExpression bExp = Expression.LessThanOrEqual(memExp, memExp2);
            return bExp;
        }

        public static Expression LessThanOrEqual(this ParameterExpression paramExpression, string propertyName,
                                                 object value, Type elementType,
                                                 Expression<Func<string, object, object>> expressionFunc)
        {
            // constructing a wrapper Func that would return Int32 value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { elementType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExpression, propertyName, expressionFunc });
            value = NullableHelperInternal.ChangeType(value, elementType, CultureInfo.InvariantCulture);
            Expression rightOperandExpression = Expression.Constant(value);
            if (rightOperandExpression.Type != elementType)
            {
                rightOperandExpression = Expression.Convert(rightOperandExpression, invokeExp.Type);
            }

            var bExp = Expression.LessThanOrEqual(invokeExp, rightOperandExpression);
            return bExp;
        }

        /// <summary>
        /// Predicate is a Binary expression that needs to be built for a single or a series
        /// of values that needs to be passed on to the WHERE expression.
        /// <para></para>
        /// <para></para>
        /// <code lang="C#">var binaryExp = queryable.Predicate(parameter,
        /// &quot;EmployeeID&quot;, &quot;4&quot;, true);</code>
        /// </summary>
        /// <remarks>
        /// First create a ParameterExpression using the Parameter extension function, then
        /// use the same ParameterExpression to generate the predicates.
        /// </remarks>
        /// <param name="source"></param>
        /// <param name="paramExpression"></param>
        /// <param name="propertyName"></param>
        /// <param name="constValue"></param>
        /// <param name="filterType"></param>
        /// <param name="filterBehaviour"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="sourceType"></param>
        public static Expression Predicate(this IQueryable source, ParameterExpression paramExpression,
                                           string propertyName, object constValue, FilterType filterType,
                                           FilterBehavior filterBehaviour, bool isCaseSensitive, Type sourceType) // Predicate1
        {
            return Predicate(source, constValue, filterType, filterBehaviour, isCaseSensitive, sourceType, null, null, paramExpression ?? null, propertyName ?? string.Empty);
        }

        public static Expression Predicate(this IQueryable source, ParameterExpression paramExpression,
                                           string propertyName, object constValue, FilterType filterType,
                                           FilterBehavior filterBehaviour, bool isCaseSensitive, Type sourceType,
            /*Expression<Func<string, object, object>>*/ Delegate expressionFunc, Type memberType = null)

        // Predicate2
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            // var checkDelg = expressionFunc.Compile();
            // invoking this delegate will return a value, that determines the type of method to be called in the Queryable class.
            // Type memberType = null;
            if (filterBehaviour == FilterBehavior.StringTyped)
            {
                memberType = typeof(string);
            }
            else
            {
                if (memberType == null)
                {
                    while (enumerator.MoveNext())
                    {
                        var returnValue = expressionFunc?.DynamicInvoke(new object[] { propertyName, enumerator.Current });
                        if (returnValue != null)
                        {
                            memberType = returnValue.GetType();
                            break;
                        }
                    }
                }

                if (memberType == null && constValue != null)
                {
                    memberType = constValue.GetType();
                }
                else if (constValue == null) // if value is null, we need to set the filterbehaviour as stringtyped.
                {
                    filterBehaviour = FilterBehavior.StringTyped;
                    memberType = typeof(string);
                }
            }

            return Predicate(source, paramExpression, propertyName, constValue, memberType, filterType, filterBehaviour,
                             isCaseSensitive, sourceType, expressionFunc);
        }

        public static Expression Predicate(this IQueryable source, ParameterExpression paramExpression,
                                           string propertyName, object constValue, Type memberType,
                                           FilterType filterType, FilterBehavior filterBehaviour, bool isCaseSensitive,
                                           Type sourceType, /*Expression<Func<string, object, object>>*/
                                           Delegate expressionFunc) ////Predicate2
        {
            if (filterBehaviour == FilterBehavior.StronglyTyped)
            {
                var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
                var wrapperFuncMethod = methods.FirstOrDefault(
                        m => m.Name == "GetDelegateInvokeExpressionAggregateFunc"
                        && m.IsStatic && m.IsPrivate && m.IsGenericMethod);

                var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { memberType });
                var memExp = (Expression)genericWrapper.Invoke(null, new object[]
                        {
                            paramExpression, propertyName, expressionFunc
                        });
                return Predicate(source, constValue, filterType, filterBehaviour, isCaseSensitive, 
                    sourceType, memberType ?? null, memExp, paramExpression ?? null, propertyName ?? string.Empty);
            }
            else
            {
                Func<Delegate, string, object, string> fun = (lambda, prop, rec) =>
                {
                    // var lambda = func.Compile();
                    var val = lambda.DynamicInvoke(new object[] { prop, rec });
                    if (val != null)
                    {
                        return val.ToString();
                    }

                    return null;
                };
                var invokeExp = Expression.Invoke(
                    Expression.Constant(fun),
                    new Expression[]
                                                  {
                                                      Expression.Constant(expressionFunc),
                                                      Expression.Constant(propertyName), paramExpression
                                                  });

                return Predicate(source, constValue, filterType, filterBehaviour, isCaseSensitive, 
                    sourceType, memberType ?? null, invokeExp, paramExpression ?? null, propertyName ?? string.Empty);
            }
        }

        private static ValueTuple<Expression, Expression, object> GetPxExpression(
            FilterType filterType, Type memberType, object value,
            bool isCaseSensitive, Expression memExp, Expression bExp
            )
        {
            var underlyingType = memberType;
            if (NullableHelperInternal.IsNullableType(memberType))
            {
                underlyingType = NullableHelperInternal.GetUnderlyingType(memberType);
            }

            if (value != null)
            {
                try
                {
                    value = ValueConvert.ChangeType((underlyingType.Name.Equals("DateTimeOffset", StringComparison.Ordinal) ? value.ToString() : value), underlyingType, CultureInfo.CurrentCulture);
                }
                catch (InvalidCastException)
                {
                }
            }
            var nullablememberType = NullableHelperInternal.GetNullableType(memberType);

            switch (filterType)
            {
                case FilterType.Equals:
                    if (isCaseSensitive || memberType != typeof(string))
                    {
                        if (value != null)
                        {
                            var exp = Expression.Constant(value, memberType);
#if !EJ2_DNX
                            if ((nullablememberType == memberType && memberType != typeof(object)) || memberType.GetTypeInfo().IsEnum)
#else
                                 if ((nullablememberType == memberType && memberType != typeof(object)) || memberType.IsEnum)
#endif
                                bExp = Expression.Equal(memExp, Expression.Constant(value, memberType));
                            else
                            {
                                bExp = Expression.Call(exp, exp.Type.GetMethod("Equals", new[] { memExp.Type }), memExp);
                            }
                        }
                        else
                        {
                            memExp = Expression.Convert(memExp, nullablememberType);
                            bExp = Expression.Equal(memExp, Expression.Constant(value, nullablememberType));
                            // bExp = Expression.Call(exp, nullablememberType.GetMethod("Equals", new[] { nullablememberType }), Expression.Constant(memExp));
                        }
                    }
                    else
                    {
                        memExp = Expression.Coalesce(memExp, Expression.Constant(value == null ? "blanks" : string.Empty));
                        var toLowerMethodCall = memExp.ToLowerMethodCallExpression();
                        bExp = Expression.Equal(toLowerMethodCall,
                                                Expression.Constant(
                                                    value == null ? "blanks" : value.ToString().ToLowerInvariant(),
                                                    typeof(string)));
                    }

                    break;
                case FilterType.NotEquals:
                    if (isCaseSensitive || memberType != typeof(string))
                    {
                        if (value != null)
                        {
                            bExp = Expression.NotEqual(memExp, Expression.Constant(value, memberType));
                        }
                        else
                        {
                            memExp = Expression.Convert(memExp, nullablememberType);
                            bExp = Expression.NotEqual(memExp, Expression.Constant(value, nullablememberType));
                        }
                    }
                    else
                    {
                        memExp = Expression.Coalesce(memExp, Expression.Constant(value == null ? "blanks" : string.Empty));
                        var toLowerMethodCall = memExp.ToLowerMethodCallExpression();
                        bExp = Expression.NotEqual(toLowerMethodCall,
                                                   Expression.Constant(
                                                       value == null ? "blanks" : value.ToString().ToLowerInvariant(),
                                                       memberType));
                    }

                    break;
                case FilterType.LessThan:
                    if (value != null)
                    {
                        bExp = Expression.LessThan(memExp, Expression.Constant(value, memberType));
                    }
                    else
                    {
                        memExp = Expression.Convert(memExp, nullablememberType);
                        bExp = Expression.LessThan(memExp, Expression.Constant(value, nullablememberType));
                    }
                    break;
                case FilterType.LessThanOrEqual:
                    if (value != null)
                    {
                        bExp = Expression.LessThanOrEqual(memExp, Expression.Constant(value, memberType));
                    }
                    else
                    {
                        memExp = Expression.Convert(memExp, nullablememberType);
                        bExp = Expression.LessThanOrEqual(memExp, Expression.Constant(value, nullablememberType));
                    }
                    break;
                case FilterType.GreaterThan:
                    if (value != null)
                    {
                        bExp = Expression.GreaterThan(memExp, Expression.Constant(value, memberType));
                    }
                    else
                    {
                        memExp = Expression.Convert(memExp, nullablememberType);
                        bExp = Expression.GreaterThan(memExp, Expression.Constant(value, nullablememberType));
                    }
                    break;
                case FilterType.GreaterThanOrEqual:
                    if (value != null)
                    {
                        bExp = Expression.GreaterThanOrEqual(memExp, Expression.Constant(value, memberType));
                    }
                    else
                    {
                        memExp = Expression.Convert(memExp, nullablememberType);
                        bExp = Expression.GreaterThanOrEqual(memExp, Expression.Constant(value, nullablememberType));
                    }
                    break;
            }
            return (memExp, bExp, value);
        }

        private static ValueTuple<Expression, Expression, object> GetPxxExpression(FilterType filterType, 
            Expression memExp, Expression bExp, object value, bool isCaseSensitive, Type memberType, object constValue)
        {
            if (!isCaseSensitive && (filterType == FilterType.Equals || filterType == FilterType.NotEquals))
            {
                value = NullableHelperInternal.FixDbNUllasNull(constValue, memberType);
            }
            var toString = memExp.Type.GetMethods().FirstOrDefault(d => d.Name == "ToString");
            // if (NullableHelperInternal.IsNullableType(memberType) || memberType == typeof(string))
            string stringValue = string.Empty;
            if (memberType == typeof(string))
            {
                memExp = value == null || string.Equals("null", value.ToString(), StringComparison.Ordinal) ? Expression.Coalesce(memExp, Expression.Constant("Blanks")) : Expression.Coalesce(memExp, Expression.Constant(string.Empty));
                stringValue = value == null || string.Equals("null", value.ToString(), StringComparison.Ordinal) ? "Blanks" : value.ToString();
            }
            else
            {
                stringValue = value == null ? "" : value.ToString();
            }

            if (memberType.Name != "String")
            {
                memExp = Expression.Call(memExp, toString);
            }
            switch (filterType)
            {
                case FilterType.NotEquals:
                    if (!isCaseSensitive)
                    {
                        memExp = ToLowerMethodCallExpression(memExp);
                        bExp = Expression.NotEqual(memExp,
                                                    Expression.Constant(stringValue.ToLowerInvariant(), typeof(string)));
                    }
                    else
                    {
                        bExp = Expression.NotEqual(memExp, Expression.Constant(stringValue, typeof(string)));
                    }

                    break;
                case FilterType.Equals:
                case FilterType.StartsWith:
                case FilterType.Contains:
                case FilterType.EndsWith:
                    var stringMethod = typeof(string).GetMethod(filterType.ToString(), new[] { memExp.Type });
                    if (isCaseSensitive)
                    {
                        bExp = Expression.Call(memExp, stringMethod, new Expression[] { Expression.Constant(stringValue, typeof(string)) });
                    }
                    else
                    {
                        var toLowerMethod = ToLowerMethodCallExpression(memExp);
                        bExp = Expression.Call(toLowerMethod, stringMethod,
                                    new Expression[]
                                    {
                                            Expression.Constant(stringValue.ToLowerInvariant(), typeof (string))
                                    });
                    }

                    break;
            }

            return (memExp, bExp, value);
        }

        private static Expression Predicate(this IQueryable source, object constValue, FilterType filterType,
                                           FilterBehavior filterBehaviour, bool isCaseSensitive, Type sourceType, Type memberType, Expression memExp, ParameterExpression paramExpression, string propertyName)
        {
            var hasExpressionFunc = false; _ = filterBehaviour;
            string[] propertyNameList = null;
            int propCount = 1;
            if (memExp == null)
            {
                memExp = paramExpression.GetValueExpression(propertyName, sourceType);
                memberType = memExp.Type;
                propertyNameList = propertyName.Split('.');
                propCount = propertyNameList.Length;
            }
            else
            {
                hasExpressionFunc = true;
            }

            var value = constValue;
            Expression bExp = null;
            if (memberType == typeof(DateTime?) && value != null && DateTime.TryParse(value.ToString(), out var newdatetime))
            {
                var dateAndTime = newdatetime.TimeOfDay.TotalSeconds;
                var hasVal = Expression.Property(memExp, nameof(Nullable<DateTime>.HasValue));
                var dateVal = Expression.Property(memExp, nameof(Nullable<DateTime>.Value));
                var propertyDate = (dateAndTime == 0) ? Expression.Property(dateVal, nameof(DateTime.Date)) : dateVal;
                memExp = Expression.Condition(Expression.Not(hasVal), Expression.Constant(null, typeof(DateTime?)), Expression.Convert(propertyDate, typeof(DateTime?)));
            }

            if (memberType == typeof(DateTimeOffset?) && value != null && DateTimeOffset.TryParse(value.ToString(), out var newdatetimeoffset))
            {
                var dateAndTime = newdatetimeoffset.TimeOfDay.TotalSeconds;
                var hasVal = Expression.Property(memExp, nameof(Nullable<DateTimeOffset>.HasValue));
                var dateVal = Expression.Property(memExp, nameof(Nullable<DateTimeOffset>.Value));
                var propertyDate = (dateAndTime == 0) ? Expression.Property(dateVal, nameof(DateTimeOffset.Date)) : dateVal;
                memExp = Expression.Condition(Expression.Not(hasVal), Expression.Constant(null, typeof(DateTimeOffset?)), Expression.Convert(propertyDate, typeof(DateTimeOffset?)));
            }

            if (filterType == FilterType.Equals || filterType == FilterType.NotEquals ||
                 filterType == FilterType.LessThan || filterType == FilterType.LessThanOrEqual ||
                 filterType == FilterType.GreaterThan || filterType == FilterType.GreaterThanOrEqual)
            {
                
                ValueTuple<Expression, Expression, object> v = GetPxExpression(filterType, memberType, value, isCaseSensitive, memExp, bExp);
                memExp = v.Item1; 
                bExp = v.Item2;
                value = v.Item3;
            }
            else
            {

                ValueTuple<Expression, Expression, object> v = GetPxxExpression(filterType, memExp, bExp, value, 
                    isCaseSensitive, memberType, constValue);
                memExp = v.Item1;
                bExp = v.Item2;
                value = v.Item3;
            }

            // Coding for complex property
            if (!hasExpressionFunc && propCount > 1)
            {
                Expression basenullexp = null;
                Expression basenotnullexp = null;
                Expression propExp = paramExpression;

                var valueExp = Expression.Constant(value);
                var nullExp = Expression.Constant(null);
                var Exp = Expression.Convert(valueExp, typeof(object));
                var nullvalExp = Expression.Equal(Exp, nullExp);
                var notnullvalExp = Expression.NotEqual(Exp, nullExp);

                for (int prop = 0; prop < propCount - 1; prop++)
                {
                    if (!string.Equals(propExp.Type.Name, "ExpandoObject", StringComparison.Ordinal) && !propExp.Type.IsSubclassOf(typeof(DynamicObject)))
                    {
                        propExp = Expression.PropertyOrField(propExp, propertyNameList[prop]);
                        var tempnullexp = Expression.Equal(propExp, nullExp);
                        if (basenullexp == null)
                        {
                            basenullexp = tempnullexp;
                        }
                        else
                        {
                            basenullexp = Expression.OrElse(basenullexp, tempnullexp);
                        }

                        var tempnotnullexp = Expression.NotEqual(propExp, nullExp);
                        if (basenotnullexp == null)
                        {
                            basenotnullexp = tempnotnullexp;
                        }
                        else
                        {
                            basenotnullexp = Expression.AndAlso(basenotnullexp, tempnotnullexp);
                        }
                    }
                }

                if (basenullexp != null)
                {
                    if (filterType == FilterType.Equals)
                    {
                        basenullexp = basenullexp.AndAlsoPredicate(nullvalExp);
                    }
                    else if (filterType == FilterType.NotEquals)
                    {
                        basenullexp = basenullexp.AndAlsoPredicate(notnullvalExp);
                    }
                    else if (filterType != FilterType.StartsWith && filterType != FilterType.EndsWith)
                    {
                        bExp = basenullexp.OrElsePredicate(bExp);
                    }

                    bExp = basenotnullexp.AndAlsoPredicate(bExp);
                }
            }

            return bExp;
        }

        public static Expression Predicate(this IQueryable source, ParameterExpression paramExpression,
                                           string propertyName, object constValue, FilterType filterType,
                                           FilterBehavior filteBehaviour, bool isCaseSensitive, Type sourceType,
                                           string format) // Predicate3
        {
            Expression memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var memberType = memExp.Type; _ = filteBehaviour;
            var underlyingType = memberType;

            if (NullableHelperInternal.IsNullableType(memberType))
            {
                underlyingType = NullableHelperInternal.GetUnderlyingType(memberType);
            }

            var value = NullableHelperInternal.FixDbNUllasNull(constValue, memberType);
            if (value != null)
            {
                if (memberType.Name == "Boolean")
                {
                    if ("true".Contains(value.ToString().ToLowerInvariant(), StringComparison.Ordinal))
                    {
                        value = "1";
                    }
                    else if ("false".Contains(value.ToString().ToLowerInvariant(), StringComparison.Ordinal))
                    {
                        value = "0";
                    }
                }
            }

            if (filterType == FilterType.Equals || filterType == FilterType.NotEquals ||
                filterType == FilterType.LessThan || filterType == FilterType.LessThanOrEqual ||
                filterType == FilterType.GreaterThan || filterType == FilterType.GreaterThanOrEqual)
            {
                ValueTuple<Expression, object> v = GetPExpression(filterType, underlyingType,
                    format, memExp, value, memberType, isCaseSensitive);
                Expression bExp = v.Item1; value = v.Item2;
                return bExp;
            }
            else
            {
                var toString = memExp.Type.GetMethods().FirstOrDefault(d => d.Name == "ToString");
                memExp = Expression.Call(memExp, toString);

                Expression coalesceExp = Expression.Coalesce(memExp, Expression.Constant(string.Empty));
                var stringMethod = typeof(string).GetMethods().FirstOrDefault(m => m.Name == filterType.ToString());

                if (isCaseSensitive)
                {
                    var methodCallExp = Expression.Call(coalesceExp, stringMethod,
                        new Expression[] { Expression.Constant(value, typeof(string)) });
                    return methodCallExp;
                }
                else
                {
                    var toLowerMethodCall = ToLowerMethodCallExpression(coalesceExp);
                    var methodCallExp = Expression.Call(
                        toLowerMethodCall,
                        stringMethod,
                        new Expression[] { Expression.Constant(value == null ? null : value.ToString().ToLowerInvariant(), typeof(string)) });
                    return methodCallExp;
                }

            }
        }

        private static ValueTuple<Expression, object> GetPExpression(FilterType filterType,
            Type underlyingType, string format, Expression memExp, object value, Type memberType, bool isCaseSensitive)
        {
            Expression bExp = null;
            switch (filterType)
            {
                case FilterType.Equals:
                    if (underlyingType != typeof(string))
                    {
                        if (!string.IsNullOrEmpty(format))
                        {
                            var formatMethodCall = GetFormatMethodCallExpression(memExp, format);
                            bExp = Expression.Equal(formatMethodCall, Expression.Constant(value, typeof(string)));
                        }
                        else
                        {
                            ConstantExpression exp = Expression.Constant(value, memberType);
                            // Expression.Equal can't compare DBNull. So Expression.Call used to compare DBNull value.
#if !EJ2_DNX
                            if (value != null && !memberType.GetTypeInfo().IsEnum)
#else
                               if (value != null && !memberType.IsEnum)
#endif
                                bExp = Expression.Call(exp, exp.Type.GetMethod("Equals", new[] { memExp.Type }), memExp);
                            else
                            {
                                bExp = Expression.Equal(memExp, exp);
                            }
                        }
                    }
                    else
                    {
                        if (isCaseSensitive)
                        {
                            ConstantExpression exp = Expression.Constant(value, memberType);
                            // Expression.Equal can't compare DBNull. So Expression.Call used to compare DBNull value.
                            if (value != null)
                            {
                                bExp = Expression.Call(exp, exp.Type.GetMethod("Equals", new[] { memExp.Type }), memExp);
                            }
                            else
                            {
                                bExp = Expression.Equal(memExp, Expression.Constant(value, memberType));
                            }
                        }
                        else
                        {
                            var toLowerMethodCall =
                                ToLowerMethodCallExpression(Expression.Coalesce(memExp,
                                                                                   Expression.Constant(string.Empty)));
                            bExp = Expression.Equal(toLowerMethodCall,
                                                    Expression.Constant(
                                                        value == null ? null : value.ToString().ToLowerInvariant(),
                                                        memExp.Type));
                        }
                    }

                    break;
                case FilterType.NotEquals:
                    if (underlyingType != typeof(string))
                    {
                        if (!string.IsNullOrEmpty(format))
                        {
                            var formatMethodCall = GetFormatMethodCallExpression(memExp, format);
                            bExp = Expression.NotEqual(formatMethodCall, Expression.Constant(value, typeof(string)));
                        }
                        else
                        {
                            bExp = Expression.NotEqual(memExp, Expression.Constant(value, memberType));
                        }
                    }
                    else
                    {
                        if (isCaseSensitive)
                        {
                            bExp = Expression.NotEqual(memExp, Expression.Constant(value, memberType));
                        }
                        else
                        {
                            var toLowerMethodCall = ToLowerMethodCallExpression(Expression.Coalesce(memExp, Expression.Constant(string.Empty)));
                            bExp = Expression.NotEqual(toLowerMethodCall, Expression.Constant(value == null ? null : value.ToString().ToLowerInvariant(), memberType));
                        }
                    }

                    break;
                case FilterType.LessThan:
                    if (!string.IsNullOrEmpty(format))
                    {
                        value = ValueConvert.ChangeType(value, underlyingType, CultureInfo.CurrentCulture, format, true);
                        bExp = Expression.LessThan(memExp, Expression.Constant(value, memberType));
                    }
                    else
                    {
                        bExp = Expression.LessThan(memExp, Expression.Constant(value, memberType));
                    }

                    break;
                case FilterType.LessThanOrEqual:
                    if (!string.IsNullOrEmpty(format))
                    {
                        var formatMethodCall = GetFormatMethodCallExpression(memExp, format);
                        Expression eqbExp = Expression.Equal(formatMethodCall,
                                                             Expression.Constant(value, typeof(string)));
                        value = ValueConvert.ChangeType(value, underlyingType, CultureInfo.CurrentCulture, format,
                                                        true);
                        bExp = Expression.Or(Expression.LessThan(memExp, Expression.Constant(value, memberType)),
                                             eqbExp);
                    }
                    else
                    {
                        bExp = Expression.LessThanOrEqual(memExp, Expression.Constant(value, memberType));
                    }

                    break;
                case FilterType.GreaterThan:
                    if (!string.IsNullOrEmpty(format))
                    {
                        var formatMethodCall = GetFormatMethodCallExpression(memExp, format);
                        Expression notbExp = Expression.NotEqual(formatMethodCall,
                                                                 Expression.Constant(value, typeof(string)));
                        value = ValueConvert.ChangeType(value, underlyingType, CultureInfo.CurrentCulture, format,
                                                        true);
                        bExp = Expression.And(
                            Expression.GreaterThan(memExp, Expression.Constant(value, memberType)), notbExp);
                    }
                    else
                    {
                        bExp = Expression.GreaterThan(memExp, Expression.Constant(value, memberType));
                    }

                    break;
                case FilterType.GreaterThanOrEqual:
                    value = ValueConvert.ChangeType(value, underlyingType, CultureInfo.CurrentCulture, format, true);
                    bExp = Expression.GreaterThanOrEqual(memExp, Expression.Constant(value, memberType));
                    break;
            }

            return (bExp, value);
        }

        private static MethodCallExpression ToLowerMethodCallExpression(this Expression memExp)
        {
            var tolowerMethod = typeof(string).GetMethods().FirstOrDefault(m => m.Name == "ToLower");
            var toLowerMethodCall = Expression.Call(memExp, tolowerMethod, Array.Empty<Expression>());
            return toLowerMethodCall;
        }

        private static MethodCallExpression ToStringMethodCallExpression(this Expression memExp)
        {
            var toString = memExp.Type.GetMethods().FirstOrDefault(d => d.Name == "ToString");
            var coalesceExp = Expression.Coalesce(memExp, Expression.Constant(string.Empty));
            return Expression.Call(coalesceExp, toString);
        }

        private static MethodCallExpression GetFormatMethodCallExpression(Expression memExp, string format)
        {
#if EJ2_DNX
            if (memExp.Type.IsGenericType && memExp.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
#else
            if (memExp.Type.GetTypeInfo().IsGenericType && memExp.Type.GetTypeInfo().GetGenericTypeDefinition() == typeof(Nullable<>))
#endif
            {
                memExp = Expression.Call(memExp, "GetValueOrDefault", EmptyTypes);
            }

            var formatMethod = typeof(DateTime).GetMethod(
                "ToString",
                new Type[] { typeof(string), typeof(IFormatProvider) });
            if (memExp.Type == typeof(decimal))
            {
                formatMethod = typeof(decimal).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            else if (memExp.Type == typeof(double))
            {
                formatMethod = typeof(double).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            else if (memExp.Type == typeof(float))
            {
                formatMethod = typeof(float).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            else if (memExp.Type == typeof(short))
            {
                formatMethod = typeof(short).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            else if (memExp.Type == typeof(int))
            {
                formatMethod = typeof(int).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }
            else if (memExp.Type == typeof(long))
            {
                formatMethod = typeof(long).GetMethod(
                    "ToString",
                    new Type[] { typeof(string), typeof(IFormatProvider) });
            }

            var formatMethodCall = Expression.Call(
                memExp,
                formatMethod, Expression.Constant(format), Expression.Constant(CultureInfo.CurrentCulture));
            return formatMethodCall;
        }

        /// <summary>
        /// Generates a Select query for a single property value.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType">Type.</param>
        public static IQueryable Select(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            Expression memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            LambdaExpression lambda = Expression.Lambda(memExp, paramExpression);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Select",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
        }

        public static IQueryable Select(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Select(propertyName, sourceType);
        }

        /// <summary>
        /// Generates a Select query for a single and multiple property value.
        /// </summary>
        /// <typeparam name="T">Type of the data source elements.</typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType">Type.</param>
        public static IQueryable Select<T>(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var xParameter = Expression.Parameter(typeof(T), "o"); _ = sourceType;
            var xNew = Expression.New(typeof(T));
            var bindings = propertyName?.Split(',').Select(o => o.Trim()).Select(o =>
            {
                var mi = typeof(T).GetProperty(o);
                var xOriginal = Expression.Property(xParameter, mi);
                return Expression.Bind(mi, xOriginal);
            });
            var xInit = Expression.MemberInit(xNew, bindings);
            LambdaExpression lambda = Expression.Lambda<Func<T, T>>(xInit, xParameter);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Select",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
        }

        public static IQueryable Select<T>(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Select<T>(propertyName, sourceType);
        }

        ///// <summary>
        ///// Generates a Select query based on the properties passed.
        ///// <para></para>
        ///// <code lang="C#">            DataClasses1DataContext db = new
        ///// DataClasses1DataContext();
        /////             var orders = db.Orders.Skip(0).Take(10).ToList();
        /////             var queryable = orders.AsQueryable();
        /////             var selector = queryable.Select(new string[]{
        ///// &quot;OrderID&quot;, &quot;ShipCountry&quot; });</code>
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="properties"></param>
        // public static IQueryable Select(this IQueryable source, params string[] properties)
        // {
        //    return Select(source, properties.ToList());
        // }

        ///// <summary>
        ///// Generates a Select query based on the properties passed.
        ///// <para></para>
        ///// <code lang="C#">            DataClasses1DataContext db = new
        ///// DataClasses1DataContext();
        /////             var orders = db.Orders.Skip(0).Take(10).ToList();
        /////             var queryable = orders.AsQueryable();
        /////             var selector = queryable.Select(new List&lt;string&gt;() {
        ///// &quot;OrderID&quot;, &quot;ShipCountry&quot; });</code>
        ///// <para></para>
        ///// <para>It returns a dynamic class generated thru ReflectionEmit, Use reflection
        ///// to identify the properties and values.</para>
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="properties"></param>
        ///// <param name="sourceType"></param>
        // public static IQueryable Select(this IQueryable source, IEnumerable<string> properties, Type sourceType)
        // {
        //    var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
        //    var selector = GenerateNew(properties, paramExpression);
        //    var lambda = Expression.Lambda(selector, paramExpression);
        //    return source.Provider.CreateQuery(
        //        Expression.Call(
        //            typeof(Queryable),
        //            "Select",
        //            new Type[] { source.ElementType, lambda.Body.Type },
        //            source.Expression,
        //            lambda));
        // }

        // public static IQueryable Select(this IQueryable source, IEnumerable<string> properties)
        // {
        //    var sourceType = source.ElementType;
        //    return source.Select(properties, sourceType);
        // }

        /// <summary>
        /// Generates a SKIP expression in the IQueryable source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="constValue">The const value.</param>
        /// <param name="sourceType">Type.</param>
        /// <returns></returns>
        public static IQueryable Skip(this IQueryable source, int constValue, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            _ = sourceType;
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Skip",
                    new Type[] { source.ElementType },
                    new Expression[] { source.Expression, Expression.Constant(constValue) }));
        }

        public static IQueryable Skip(this IQueryable source, int constValue)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Skip(constValue, sourceType);
        }

        #region Aggregate extensions

        // Func to calculate the summary aggregates when UseBindingValue is true.
        private static Expression GetInvokeExpressionAggregateFuncSummaryCalculation<TResult>(ParameterExpression paramExp, string propertyName,
                                                                                                                Expression<Func<string, object, object>> expressionFunc)
        {
            Func<Expression<Func<string, object, object>>, string, object, TResult> fun = (func, prop, rec) =>
            {
                var lambda = func.Compile();
                object tempValue = lambda.DynamicInvoke(new object[] { prop, rec });

                // if we use UseBindingValue as true in column with nullable values, zero will be consider for null values.
                if (tempValue == null)
                {
                    tempValue = NullableHelperInternal.ChangeType(0, typeof(TResult), CultureInfo.InvariantCulture);
                }

                TResult val = (TResult)tempValue;
                return val;
            };

            // Expression<Func<Expression<Func<string, object, object>>, string, object, TResult>> eIFunc =
            //    (func, prop, rec) => fun(func, prop, rec);
            var invokeExp = Expression.Invoke(
                Expression.Constant(fun),
                new Expression[]
                                                  {
                                                      Expression.Constant(expressionFunc),
                                                      Expression.Constant(propertyName), paramExp
                                                  });
            return invokeExp;
        }

        private static Expression GetInvokeExpressionAggregateFunc<TResult>(
            ParameterExpression paramExp,
            string propertyName,
            Expression<Func<string, object, object>>
                                                                                expressionFunc)
        {
            // constructing a wrapper Func that would return a generic value
            Func<Expression<Func<string, object, object>>, string, object, TResult> fun = (func, prop, rec) =>
                {
                    var lambda = func.Compile();
                    TResult val = (TResult)lambda.DynamicInvoke(new object[] { prop, rec });
                    return val;
                };

            // Expression<Func<Expression<Func<string, object, object>>, string, object, TResult>> eIFunc =
            //    (func, prop, rec) => fun(func, prop, rec);
            var invokeExp = Expression.Invoke(
                Expression.Constant(fun),
                new Expression[]
                                                  {
                                                      Expression.Constant(expressionFunc),
                                                      Expression.Constant(propertyName), paramExp
                                                  });
            return invokeExp;
        }

        /// <summary>
        /// Use this method with a cached delegate, this improves performance when using complex Expressions.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="paramExp"></param>
        /// <param name="propertyName"></param>
        /// <param name="expressionFunc"></param>
        /// <returns></returns>
        private static Expression GetDelegateInvokeExpressionAggregateFunc<TResult>(ParameterExpression paramExp, string propertyName, Delegate expressionFunc)
        {
            // constructing a wrapper Func that would return a generic value
            Func<Delegate, string, object, TResult> fun = (lambda, prop, rec) =>
                {
                    // var lambda = func.Compile();
                    var val = lambda.DynamicInvoke(new object[] { prop, rec });
                    if (val != null)
                    {
                        return (TResult)val;
                    }

                    return default(TResult);
                };

            // Expression<Func<Delegate, string, object, TResult>> eIFunc = (func, prop, rec) => fun(func, prop, rec);
            var invokeExp = Expression.Invoke(
                Expression.Constant(fun),
                new Expression[]
                                                  {
                                                      Expression.Constant(expressionFunc),
                                                      Expression.Constant(propertyName), paramExp
                                                  });
            return invokeExp;
        }

        // MethodInfo[] collection is calculated frequently whenever the summary value changes.
        // To prevent, this collection is calculated and it it used whenever the summary value changes.
        private static MethodInfo[] queryableSumMethod;

        private static MethodInfo[] queryableSummethod
        {
            get
            {
                if (queryableSumMethod == null)
                {
                    return queryableSumMethod = typeof(Queryable).GetMethods().Where(m => m.Name == "Sum" && m.GetParameters().Length == 2).ToArray();
                }

                return queryableSumMethod;
            }
        }

        // MethodInfo[] collection for Queryable extensions and hold the average methods other than Int32.
        private static MethodInfo[] queryableaverageMethod;

        private static MethodInfo[] queryableAverageMethod
        {
            get
            {
                if (queryableaverageMethod == null)
                {
                    return queryableaverageMethod = typeof(Queryable).GetMethods().Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
                }

                return queryableaverageMethod;
            }
        }

        // MethodInfo[] collection is calculated frequently whenever the summary value changes.
        // To prevent, this collection is calculated and it is used whenever the summary value changes.
        private static MethodInfo[] enumerablesummethods;

        private static MethodInfo[] enumerableSumMethods
        {
            get
            {
                if (enumerablesummethods == null)
                {
                    return
                        enumerablesummethods =
                        typeof(EnumerableExtensions).GetMethods()
                                                     .Where(m => m.Name == "Sum" && m.GetParameters().Length == 2)
                                                     .ToArray();
                }

                return enumerablesummethods;
            }
        }

        // MethodInfo[] collection for enumerable extensions and hold the average methods for Int32.
        private static MethodInfo[] enumerableaverageMethods;

        private static MethodInfo[] enumerableAverageMethods
        {
            get
            {
                if (enumerableaverageMethods == null)
                {
                   return enumerableaverageMethods = typeof(EnumerableExtensions).GetMethods().Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
                }

                return enumerableaverageMethods;
            }
        }

        #region Sum

        public static object Sum(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var lambda = Expression.Lambda(memExp, paramExpression);

            // Commented the below lines since it was declared as an property and used whenever needed.
            // var tmethod = typeof(Queryable).GetMethods()
            //    .Where(m => m.Name == "Sum" && m.GetParameters().Length == 2).ToArray();
            var bodyType = lambda.Body.Type;

            // Get the exact method based on the bodyType.
            MethodInfo method = GetQueryableSumMethod(bodyType);

            // if we use property type as short, Queryable not having the own method to deal Int16 type methods. so here, get the methods from EnumerableExtensions.
            if (method == null &&
                (bodyType.Name == "Int16" || NullableHelperInternal.GetUnderlyingType(bodyType).Name == "Int16"))
            {
                // Commented the below lines since it was declared as an property and used whenever needed.
                // var eMethods = typeof(EnumerableExtensions).GetMethods().Where(m => m.Name == "Sum" && m.GetParameters().Length == 2).ToArray();
                if (NullableHelperInternal.IsNullableType(bodyType))
                {
                    method = enumerableSumMethods[2];
                }
                else
                {
                    method = enumerableSumMethods[0];
                }
            }

            if (method != null)
            {
                return
                    source.Provider.Execute(Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType }),
                    new Expression[] { source.Expression, Expression.Quote(lambda) }));
            }

            return null;
        }

        public static object Sum(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            return source.Sum(propertyName, source.ElementType);
        }

        public static object Sum(this IQueryable source, string propertyName, Expression<Func<string, object, object>> expressionFunc, Expression<Func<string, object, object>> typeFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            Type bodyType;

            // Commented the below lines since it was declared as an property and used whenever needed.
            ////var tmethod = typeof(Queryable).GetMethods().Where(m => m.Name == "Sum" && m.GetParameters().Length == 2).ToArray();
            // determine the return type, we expect the sequence to be of same type from the expression and hence get the first value and simply take its type
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            if (typeFunc != null)
            {
                var checkDelg = typeFunc.Compile();
                bodyType = (Type)checkDelg.DynamicInvoke(new object[] { propertyName, enumerator.Current });
            }
            else
            {
                var checkDelg = expressionFunc?.Compile();
                var returnValue = checkDelg?.DynamicInvoke(new object[] { propertyName, enumerator.Current });
                bodyType = returnValue?.GetType();
            }

            MethodInfo method = GetQueryableAverageMethod(bodyType);
            if (method == null && bodyType.Name == "Int16")
            {
                var eMethods = typeof(EnumerableExtensions).GetMethods().Where(m => m.Name == "Sum" && m.GetParameters().Length == 2).ToArray();
                if (NullableHelperInternal.IsNullableType(bodyType))
                {
                    method = eMethods[1];
                }
                else
                {
                    method = eMethods[0];
                }
            }

            if (method != null)
            {
                var sourceType = source.ElementType;
                var paramExp = Expression.Parameter(sourceType, sourceType?.Name);
                var cExp = Expression.Constant(propertyName);
                var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExp });

                // constructing a wrapper Func that would return Int32 value
                var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
                var wrapperFuncMethod = methods.FirstOrDefault(m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
                var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { bodyType });
                var invokeExp = (Expression)genericWrapper.Invoke(null, new object[] { paramExp, propertyName, expressionFunc });
                LambdaExpression lExp = Expression.Lambda(invokeExp, paramExp);
                var sumMethodCallExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType }), new
        Expression[]
        {
            source.Expression, Expression.Quote(lExp)
        });
                var result = source.Provider.Execute(sumMethodCallExp);
                return result;
            }

            return 0;
        }

        #endregion

        #region GetMethodInfo method

        /// <summary>
        /// Get the exact Sum method from Queryable based on body type.
        /// </summary>
        /// <param name="bodyType"></param>
        /// <returns>exact method info.</returns>
        private static MethodInfo GetQueryableSumMethod(Type bodyType)
        {
            MethodInfo method = null;
            if (NullableHelperInternal.IsNullableType(bodyType))
            {
                var originalType = NullableHelperInternal.GetUnderlyingType(bodyType);
                switch (originalType.Name)
                {
                    case "Int32":
                        method = queryableSummethod[1];
                        break;
                    case "Int64":
                        method = queryableSummethod[3];
                        break;
                    case "Single":
                        method = queryableSummethod[5];
                        break;
                    case "Double":
                        method = queryableSummethod[7];
                        break;
                    case "Decimal":
                        method = queryableSummethod[9];
                        break;
                }
            }
            else
            {
                switch (bodyType.Name)
                {
                    case "Int32":
                        method = queryableSummethod[0];
                        break;
                    case "Int64":
                        method = queryableSummethod[2];
                        break;
                    case "Single":
                        method = queryableSummethod[4];
                        break;
                    case "Double":
                        method = queryableSummethod[6];
                        break;
                    case "Decimal":
                        method = queryableSummethod[8];
                        break;
                }
            }

            return method;
        }

        /// <summary>
        /// Get the exact Average method from Queryable based on body type.
        /// </summary>
        /// <param name="bodyType"></param>
        /// <returns>exact method info.</returns>
        private static MethodInfo GetQueryableAverageMethod(Type bodyType)
        {
            MethodInfo method = null;
            if (NullableHelperInternal.IsNullableType(bodyType))
            {
                var originalType = NullableHelperInternal.GetUnderlyingType(bodyType);
                var nullableAvgMethods = queryableAverageMethod.Where(m => NullableHelperInternal.IsNullableType(m.ReturnType)).ToArray();
                switch (originalType.Name)
                {
                    case "Int32":
                        method = nullableAvgMethods[0];
                        break;
                    case "Single":
                        method = nullableAvgMethods[1];
                        break;
                    case "Int64":
                        method = nullableAvgMethods[2];
                        break;
                    case "Double":
                        method = nullableAvgMethods[3];
                        break;
                    case "Decimal":
                        method = nullableAvgMethods[4];
                        break;
                }
            }
            else
            {
                //OrderBy is used to fix ordering issue between .NET 5 and .NET 6.
                var avgMethods = queryableAverageMethod.Where(m => !NullableHelperInternal.IsNullableType(m.ReturnType)).
                    OrderBy(minfo => minfo.ToString()).ToArray();
                switch (bodyType.Name)
                {
                    case "Int32":
                        method = avgMethods[1];
                        break;
                    case "Single":
                        method = avgMethods[3];
                        break;
                    case "Int64":
                        method = avgMethods[2];
                        break;
                    case "Double":
                        method = avgMethods[0];
                        break;
                    case "Decimal":
                        method = avgMethods[4];
                        break;
                }
            }

            return method;
        }

        #endregion

        #region Average

        public static object Average(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Average(propertyName, sourceType);
        }

        public static object Average(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var lambda = Expression.Lambda(memExp, paramExpression);

            // Commented the below lines since it was declared as an property and used whenever needed.
            // var tmethod = typeof(Queryable).GetMethods()
            //                                .Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
            var bodyType = lambda.Body.Type;

            // Get the exact method based on the bodyType.
            MethodInfo method = GetQueryableAverageMethod(bodyType);

            // if we use property type as short, Queryable not having the own method to deal Int16 type methods. so here, we are getting the methods from EnumerableExtensions.
            if (method == null &&
                (bodyType.Name == "Int16" || NullableHelperInternal.GetUnderlyingType(bodyType).Name == "Int16"))
            {
                // Commented the below lines since it was declared as an property and used whenever needed.
                // var eMethods = typeof(EnumerableExtensions).GetMethods().Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
                if (NullableHelperInternal.IsNullableType(bodyType))
                {
                    method = enumerableAverageMethods[2];
                }
                else
                {
                    method = enumerableAverageMethods[0];
                }
            }

            if (method != null)
            {
                return source.Provider.Execute(Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType }),
                            new Expression[] { source.Expression, Expression.Quote(lambda) }));
            }

            return null;
        }

        public static object Average(this IQueryable source, string propertyName, Expression<Func<string, object, object>> expressionFunc, Expression<Func<string, object, object>> typeFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            Type bodyType;
            var tmethod = typeof(Queryable).GetMethods().Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
            // determine the return type, we expect the sequence to be of same type from the expression and hence get the first value and simply take its type
            var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return null;
            }

            if (typeFunc != null)
            {
                var checkDelg = typeFunc.Compile();
                bodyType = (Type)checkDelg.DynamicInvoke(new object[] { propertyName, enumerator.Current });
            }
            else
            {
                var checkDelg = expressionFunc?.Compile();
                var returnValue = checkDelg?.DynamicInvoke(new object[] { propertyName, enumerator.Current });
                bodyType = returnValue?.GetType();
            }

            MethodInfo method = GetQueryableAverageMethod(bodyType);
            if (method == null && bodyType.Name == "Int16")
            {
                var eMethods = typeof(EnumerableExtensions).GetMethods().Where(m => m.Name == "Average" && m.GetParameters().Length == 2).ToArray();
                if (NullableHelperInternal.IsNullableType(bodyType))
                {
                    method = eMethods[1];
                }
                else
                {
                    method = eMethods[0];
                }
            }

            if (method != null)
            {
                var sourceType = source.ElementType;
                var paramExp = Expression.Parameter(sourceType, sourceType?.Name);
                var cExp = Expression.Constant(propertyName);
                var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExp });

                // constructing a wrapper Func that would return Int32 value
                var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
                var wrapperFuncMethod = methods.FirstOrDefault(m => m.Name == "GetInvokeExpressionAggregateFunc" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
                var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { bodyType });
                var invokeExp = (Expression)genericWrapper.Invoke(null, new object[] { paramExp, propertyName, expressionFunc });
                LambdaExpression lExp = Expression.Lambda(invokeExp, paramExp);
                var avgMethodCallExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType }), new
        Expression[]
        {
            source.Expression, Expression.Quote(lExp)
        });
                var result = source.Provider.Execute(avgMethodCallExp);
                return result;
            }

            return 0;
        }

        #endregion

        #region Max

        public static object Max(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Max(propertyName, sourceType);
        }

        public static object Max(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var lambda = Expression.Lambda(memExp, paramExpression);
            var method = typeof(Queryable).GetMethods().Where(m => m.Name == "Max").LastOrDefault();
            var methodExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType, lambda.Body.Type }),
                                            new Expression[] { source.Expression, lambda });
            return source.Provider.Execute(methodExp);
        }

        public static object Max(this IQueryable source, string propertyName,
                                 Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var enumerator = source.GetEnumerator();
            if (enumerator == null)
            {
                return null;
            }

            var checkDelg = expressionFunc?.Compile();
            Type bodyType = null;

            // invoking this delegate will return a value, that determines the type of method to be called in the Queryable class.
            while (enumerator.MoveNext())
            {
                var returnValue = checkDelg?.DynamicInvoke(new object[] { propertyName, enumerator.Current });
                if (returnValue != null)
                {
                    bodyType = returnValue.GetType();
                    break;
                }
            }

            if (bodyType == null)
            {
                return null;
            }

            var sourceType = source.ElementType;
            var paramExp = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExp });

            // constructing a wrapper Func that would return Int32 value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod =
                methods.FirstOrDefault(
                    m => m.Name == "GetInvokeExpressionAggregateFuncSummaryCalculation" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { bodyType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExp, propertyName, expressionFunc });
            LambdaExpression lExp = Expression.Lambda(invokeExp, paramExp);
            var method = typeof(Queryable).GetMethods().Where(m => m.Name == "Max").LastOrDefault();
            var maxMethodCallExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType, bodyType }),
                                                   new
                                                       Expression[]
                                                        {
                                                            source.Expression, Expression.Quote(lExp)
                                                        });
            var result = source.Provider.Execute(maxMethodCallExp);
            return result;
        }

        #endregion

        #region Min

        public static object Min(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Min(propertyName, sourceType);
        }

        public static object Min(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var lambda = Expression.Lambda(memExp, paramExpression);
            var method = typeof(Queryable).GetMethods().Where(m => m.Name == "Min").LastOrDefault();
            var methodExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType, lambda.Body.Type }),
                                            new Expression[] { source.Expression, lambda });
            return source.Provider.Execute(methodExp);
        }

        public static object Min(this IQueryable source, string propertyName, Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var enumerator = source.GetEnumerator();
            if (enumerator == null)
            {
                return null;
            }

            enumerator = source.GetEnumerator();
            var checkDelg = expressionFunc?.Compile();
            // invoking this delegate will return a value, that determines the type of method to be called in the Queryable class.
            Type bodyType = null;

            // invoking this delegate will return a value, that determines the type of method to be called in the Queryable class.
            while (enumerator.MoveNext())
            {
                var returnValue = checkDelg?.DynamicInvoke(new object[] { propertyName, enumerator.Current });
                if (returnValue != null)
                {
                    bodyType = returnValue.GetType();
                    break;
                }
            }

            if (bodyType == null)
            {
                return null;
            }

            var sourceType = source.ElementType;
            var paramExp = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExp });

            // constructing a wrapper Func that would return Int32 value
            var methods = typeof(QueryableExtensions).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).ToArray();
            var wrapperFuncMethod = methods.FirstOrDefault(m => m.Name == "GetInvokeExpressionAggregateFuncSummaryCalculation" && m.IsStatic && m.IsPrivate && m.IsGenericMethod);
            var genericWrapper = wrapperFuncMethod.MakeGenericMethod(new Type[] { bodyType });
            var invokeExp =
                (Expression)genericWrapper.Invoke(null, new object[] { paramExp, propertyName, expressionFunc });
            LambdaExpression lExp = Expression.Lambda(invokeExp, paramExp);
            var method = typeof(Queryable).GetMethods().Where(m => m.Name == "Min").LastOrDefault();
            var minMethodCallExp = Expression.Call(null, method.MakeGenericMethod(new Type[] { sourceType, bodyType }), new Expression[] { source.Expression, Expression.Quote(lExp) });
            var result = source.Provider.Execute(minMethodCallExp);
            return result;
        }

        #endregion

        #endregion

        /// <summary>
        /// Generates a TAKE expression in the IQueryable source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="constValue">The const value.</param>
        /// <param name="sourceType">Type.</param>
        /// <returns></returns>
        public static IQueryable Take(this IQueryable source, int constValue, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Take",
                    new Type[] { sourceType },
                    new Expression[] { source.Expression, Expression.Constant(constValue) }));
        }

        public static IQueryable Take(this IQueryable source, int constValue)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Take(constValue, sourceType);
        }

        /// <summary>
        /// Generates a ThenBy query for the Queryable source.
        /// <para></para>
        /// <code lang="C#">            DataClasses1DataContext db = new
        /// DataClasses1DataContext();
        ///             var orders = db.Orders.Skip(0).Take(10).ToList();
        ///             var queryable = orders.AsQueryable();
        ///             var sortedOrders = queryable.OrderBy(&quot;ShipCountry&quot;);
        ///             sortedOrders = sortedOrders.ThenBy(&quot;ShipCity&quot;);</code>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType"></param>
        public static IQueryable ThenBy(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            var lambda = GetLambdaWithComplexPropertyNullCheck(source, propertyName ?? string.Empty, paramExpression, sourceType);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "ThenBy",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
        }

        public static IQueryable ThenBy(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.ThenBy(propertyName, sourceType);
        }

        public static IQueryable ThenBy(this IQueryable source, string propertyName, IComparer<object> comparer,
                                        Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            LambdaExpression lambda = Expression.Lambda(iExp, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenBy" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenBy(this IQueryable source, string propertyName,
                                        Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            return ThenBy(source, paramExpression, iExp);
        }

        public static IQueryable ThenBy(this IQueryable source, ParameterExpression paramExpression, Expression mExp)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            LambdaExpression lambda = Expression.Lambda(mExp, paramExpression);
            var orderedSource = source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "ThenBy",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
            return orderedSource;
        }

        /// <summary>
        /// Generates an ThenBy query for the IComparer defined.
        /// <para></para>
        /// <para> </para>
        /// <code lang="C#">   public class OrdersComparer :
        /// IComparer&lt;Order&gt;
        ///     {
        ///         public int Compare(Order x, Order y)
        ///         {
        ///             return string.Compare(x.ShipCountry, y.ShipCountry);
        ///         }
        ///     }</code>
        /// <para></para>
        /// <para><code lang="C#">var sortedOrders =
        /// db.Orders.Skip(0).Take(5).ToList().ThenBy(o =&gt; o, new
        /// OrdersComparer());</code></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="comparer"></param>
        /// <param name="sourceType"></param>
        public static IQueryable ThenBy<T>(this IQueryable source, IComparer<T> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenBy" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<T>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenBy(this IQueryable source, string propertyName, IComparer<object> comparer,
                                        Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            _ = propertyName;
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenBy" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenBy<T>(this IQueryable source, IComparer<T> comparer)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.ThenBy<T>(comparer, sourceType);
        }

        /// <summary>
        /// Generates an ThenByDescending query for the IComparer defined.
        /// <para></para>
        /// <para> </para>
        /// <code lang="C#">   public class OrdersComparer :
        /// IComparer&lt;Order&gt;
        ///     {
        ///         public int Compare(Order x, Order y)
        ///         {
        ///             return string.Compare(x.ShipCountry, y.ShipCountry);
        ///         }
        ///     }</code>
        /// <para></para>
        /// <para><code lang="C#">var sortedOrders =
        /// db.Orders.Skip(0).Take(5).ToList().ThenByDescending(o =&gt; o, new
        /// OrdersComparer());</code></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="comparer"></param>
        /// <param name="sourceType"></param>
        public static IQueryable ThenByDescending<T>(this IQueryable source, IComparer<T> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<T>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenByDescending(this IQueryable source, string propertyName,
                                                  IComparer<object> comparer, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            _ = propertyName;
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // var memExp = Expression.PropertyOrField(paramExpression, propertyName);
            LambdaExpression lambda = Expression.Lambda(paramExpression, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenByDescending<T>(this IQueryable source, IComparer<T> comparer)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.ThenByDescending<T>(comparer, sourceType);
        }

        public static IQueryable ThenByDescending(this IQueryable source, string propertyName,
                                                  IComparer<object> comparer,
                                                  Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            LambdaExpression lambda = Expression.Lambda(iExp, paramExpression);
            MethodInfo method =
                typeof(Queryable).GetMethods().FirstOrDefault(m => m.Name == "ThenByDescending" && m.GetParameters().Length == 3);
            ConstantExpression conExp = Expression.Constant(comparer, typeof(IComparer<object>));
            MethodCallExpression methodExp = Expression.Call(
                null,
                method.MakeGenericMethod(new Type[] { source.ElementType, lambda.Body.Type }),
                new Expression[] { source.Expression, lambda, conExp });
            return source.Provider.CreateQuery(methodExp);
        }

        public static IQueryable ThenByDescending(this IQueryable source, string propertyName,
                                                  Expression<Func<string, object, object>> expressionFunc)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            var paramExpression = Expression.Parameter(sourceType, sourceType?.Name);
            var cExp = Expression.Constant(propertyName);
            var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, paramExpression });
            return ThenByDescending(source, paramExpression, iExp);
        }

        public static IQueryable ThenByDescending(this IQueryable source, ParameterExpression paramExpression,
                                                  Expression mExp)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            LambdaExpression lambda = Expression.Lambda(mExp, paramExpression);
            var orderedSource = source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "ThenByDescending",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
            return orderedSource;
        }

        /// <summary>
        /// Generates a ThenByDescending query for the Queryable source.
        /// <para></para>
        /// <code lang="C#">            DataClasses1DataContext db = new
        /// DataClasses1DataContext();
        ///             var orders = db.Orders.Skip(0).Take(10).ToList();
        ///             var queryable = orders.AsQueryable();
        ///             var sortedOrders = queryable.OrderBy(&quot;ShipCountry&quot;);
        ///             sortedOrders = sortedOrders.ThenByDescending(&quot;ShipCity&quot;);</code>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <param name="sourceType"></param>
        public static IQueryable ThenByDescending(this IQueryable source, string propertyName, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            ParameterExpression paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            var lambda = GetLambdaWithComplexPropertyNullCheck(source, propertyName ?? string.Empty, paramExpression, sourceType);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "ThenByDescending",
                    new Type[] { source.ElementType, lambda.Body.Type },
                    source.Expression,
                    lambda));
        }

        public static IQueryable ThenByDescending(this IQueryable source, string propertyName)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.ThenByDescending(propertyName, sourceType);
        }

        /// <summary>
        /// Generates the where expression.
        /// <para></para>
        /// <code lang="C#">            var nw = new Northwind(@&quot;Data Source =
        /// Northwind.sdf&quot;);
        ///             IQueryable queryable = nw.Orders.AsQueryable();
        ///             var filters = queryable.Where(&quot;ShipCountry&quot;,
        /// &quot;z&quot;, FilterType.Contains);
        ///             foreach (Orders item in filters)
        ///             {
        ///                 Console.WriteLine(&quot;{0}/{1}&quot;, item.OrderID,
        /// item.ShipCountry);
        ///             }</code>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value"></param>
        /// <param name="filterType"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="sourceType"></param>
        public static IQueryable Where(this IQueryable source, string propertyName, object value, FilterType filterType,
                                       bool isCaseSensitive, Type sourceType)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var paramExpression = Expression.Parameter(source.ElementType, sourceType?.Name);
            // Code for convert complex property to simple property
            var memExp = paramExpression.GetValueExpression(propertyName, sourceType);
            var underlyingType = memExp.Type;
            if (NullableHelperInternal.IsNullableType(memExp.Type))
            {
                underlyingType = NullableHelperInternal.GetUnderlyingType(memExp.Type);
            }

            if (filterType == FilterType.Equals || filterType == FilterType.NotEquals ||
                filterType == FilterType.LessThan || filterType == FilterType.LessThanOrEqual ||
                filterType == FilterType.GreaterThan || filterType == FilterType.GreaterThanOrEqual)
            {
                BinaryExpression bExp = null;
                switch (filterType)
                {
                    case FilterType.Equals:
                        if (underlyingType != typeof(string))
                        {
                            bExp = Expression.Equal(memExp, Expression.Constant(value, memExp.Type));
                        }
                        else
                        {
                            if (isCaseSensitive)
                            {
                                bExp = Expression.Equal(memExp, Expression.Constant(value, memExp.Type));
                            }
                            else
                            {
                                var toLowerMethodCall = ToLowerMethodCallExpression(memExp);
                                bExp = Expression.Equal(toLowerMethodCall,
                                                        Expression.Constant(
                                                            value == null ? null : value.ToString().ToLowerInvariant(),
                                                            memExp.Type));
                            }
                        }

                        break;
                    case FilterType.NotEquals:
                        if (underlyingType != typeof(string))
                        {
                            bExp = Expression.NotEqual(memExp, Expression.Constant(value, memExp.Type));
                        }
                        else
                        {
                            if (isCaseSensitive)
                            {
                                bExp = Expression.NotEqual(memExp, Expression.Constant(value, memExp.Type));
                            }
                            else
                            {
                                var toLowerMethodCall = ToLowerMethodCallExpression(memExp);
                                bExp = Expression.NotEqual(toLowerMethodCall,
                                                           Expression.Constant(
                                                               value == null ? null : value.ToString().ToLowerInvariant(),
                                                               memExp.Type));
                            }
                        }

                        break;
                    case FilterType.LessThan:
                        bExp = Expression.LessThan(memExp, Expression.Constant(value, memExp.Type));
                        break;
                    case FilterType.LessThanOrEqual:
                        bExp = Expression.LessThanOrEqual(memExp, Expression.Constant(value, memExp.Type));
                        break;
                    case FilterType.GreaterThan:
                        bExp = Expression.GreaterThan(memExp, Expression.Constant(value, memExp.Type));
                        break;
                    case FilterType.GreaterThanOrEqual:
                        bExp = Expression.GreaterThanOrEqual(memExp, Expression.Constant(value, memExp.Type));
                        break;
                }

                LambdaExpression lambda = Expression.Lambda(bExp, paramExpression);
                return source.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new Type[] { source.ElementType },
                        source.Expression,
                        lambda));
            }
            else
            {
                var stringMethod =
                    typeof(string).GetMethods().Where(m => m.Name == filterType.ToString()).FirstOrDefault();
                Expression methodCallExp = null;
                if (isCaseSensitive)
                {
                    methodCallExp = Expression.Call(
                        memExp,
                        stringMethod,
                        new Expression[] { Expression.Constant(value, typeof(string)) });
                }
                else
                {
                    var toLowerMethodCall = ToLowerMethodCallExpression(memExp);
                    methodCallExp = Expression.Call(
                        toLowerMethodCall,
                        stringMethod,
                        new Expression[] { Expression.Constant(value == null ? null : value.ToString().ToLowerInvariant(), typeof(string)) });
                }

                var lambda = Expression.Lambda(methodCallExp, paramExpression);
                return source.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new Type[] { source.ElementType },
                        source.Expression,
                        lambda));
            }
        }

        public static IQueryable Where(this IQueryable source, string propertyName, object value, FilterType filterType,
                                       bool isCaseSensitive)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.Where(propertyName, value, filterType, isCaseSensitive, sourceType);
        }

        public static IQueryable Page(this IQueryable source, int pageIndex, int pageSize)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            IQueryable tempSource = source;
            if (pageIndex > 0)
            {
                tempSource = tempSource.Skip(pageIndex * pageSize);
            }

            if (pageSize > 0)
            {
                tempSource = tempSource.Take(pageSize);
            }

            return tempSource;
        }

        /// <summary>
        /// Use this function to generate WHERE expression based on Predicates. The
        /// AndPredicate and OrPredicate should be used in combination to build the
        /// predicate expression which is finally passed on to this function for creating a
        /// Lambda.
        /// <para></para>
        /// <para></para>
        /// <para></para>DataClasses1DataContext db = new DataClasses1DataContext();.
        /// <para></para>            var orders = db.Orders.Skip(0).Take(100).ToList();.
        /// <para></para>            var queryable = orders.AsQueryable();.
        /// <para></para>            var parameter =
        /// queryable.Parameter(&quot;ShipCountry&quot;);.
        /// <para></para>            var binaryExp = queryable.Predicate(parameter,.
        /// <para></para>&quot;ShipCountry&quot;, &quot;USA&quot;, true);.
        /// <para></para>            var filteredOrders = queryable.Where(parameter,
        /// binaryExp);.
        /// <para></para>            foreach (var order in filteredOrders).
        /// <para></para>            {.
        /// <para></para>                Console.WriteLine(order);.
        /// <para></para>            }.
        /// <para></para>
        /// <para></para>
        /// <para></para>Build Predicates for Contains / StartsWith / EndsWith,.
        /// <para></para>
        /// <para></para>            IQueryable queryable = nw.Orders.AsQueryable();.
        /// <para></para>            var parameter = queryable.Parameter();.
        /// <para></para>            var exp1 = queryable.Predicate(parameter,
        /// &quot;ShipCountry&quot;, &quot;h&quot;, FilterType.Contains);.
        /// <para></para>            var exp2 = queryable.Predicate(parameter,
        /// &quot;ShipCountry&quot;, &quot;a&quot;, FilterType.StartsWith);.
        /// <para></para>            var andExp = exp2.OrPredicate(exp1);.
        /// <para></para>            var filters = queryable.Where(parameter, andExp);.
        /// <para></para>            foreach (Orders item in filters).
        /// <para></para>            {.
        /// <para></para>                Console.WriteLine(&quot;{0}/{1}&quot;,
        /// item.OrderID, item.ShipCountry);.
        /// <para></para>            }.
        /// <para></para>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="paramExpression"></param>
        /// <param name="predicateExpression"></param>
        public static IQueryable Where(this IQueryable source, ParameterExpression paramExpression,
                                       Expression predicateExpression)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if(predicateExpression == null)
            {
                return Enumerable.Empty<object>().AsQueryable();
            }
            var lambda = Expression.Lambda(predicateExpression, paramExpression);
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    "Where",
                    new Type[] { source.ElementType },
                    source.Expression,
                    lambda));
        }

        #region GroupByMany extensions

        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            List<SortDescription> sortFields,
            IEnumerable<Func<TElement, object>> groupSelectors)
        {
            return GroupByMany<TElement>(elements, sortFields, groupSelectors.ToArray());
        }

        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            List<SortDescription> sortFields,
            Dictionary<string, IComparer<object>> sortComparers,
            string[] properties,
            IEnumerable<Func<TElement, object>> groupSelectors)
        {
            return GroupByMany<TElement>(elements, sortFields, sortComparers, properties.ToList(),
                                         groupSelectors.ToArray());
        }

        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            List<SortDescription> sortFields,
            Dictionary<string, IComparer<object>> sortComparers,
            List<string> properties,
            params Func<TElement, object>[] groupSelectors)
        {
            if (groupSelectors != null && groupSelectors.Length > 0)
            {
                var selector = groupSelectors.First();
                var nextSelectors = groupSelectors.Skip(1).ToArray();

                var groupBy =
                    elements.GroupBy(selector).Select(
                        g => new GroupResult
                            {
                                Key = g.Key,
                                Count = g.Count(),
                                Items = g,
                                SubGroups =
                                    g.GroupByMany(sortFields.Count > 0 ? sortFields.Skip(1).ToList() : sortFields,
                                                  sortComparers,
                                                  properties.Count > 0 ? properties.Skip(0).ToList() : properties,
                                                  nextSelectors)
                            });

                if (sortFields != null && sortFields.Count > 0)
                {
                    var sortKey = sortFields.FirstOrDefault(d => d.PropertyName == properties[0]);
                    if (sortKey.PropertyName != null && sortKey != default(SortDescription)) // && sortKey.Index == 0)
                    {
                        IComparer<object> customComparer = null;
                        sortComparers?.TryGetValue(sortKey.PropertyName, out customComparer);

                        if (sortKey.Direction == ListSortDirection.Ascending)
                        {
                            if (customComparer == null)
                            {
                                groupBy = groupBy.OrderBy(g => g.Key);
                            }
                            else
                            {
                                groupBy = groupBy.OrderBy(g => g.Key, customComparer);
                            }
                        }
                        else
                        {
                            if (customComparer == null)
                            {
                                groupBy = groupBy.OrderByDescending(g => g.Key);
                            }
                            else
                            {
                                groupBy = groupBy.OrderByDescending(g => g.Key, customComparer);
                            }
                        }
                    }
                }

                return groupBy;
            }
            else
            {
                return null;
            }
        }

        // var orders = Orders.GroupBy(o => o.ShipCountry).Select(g => new { Key = g.Key, Items = g.OrderBy(o1 => o1.ShipCountry) });
        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            List<SortDescription> sortFields,
            params Func<TElement, object>[] groupSelectors)
        {
            if (groupSelectors != null && groupSelectors.Length > 0)
            {
                var selector = groupSelectors.First();
                var nextSelectors = groupSelectors.Skip(1).ToArray();

                var groupBy =
                    elements.GroupBy(selector).Select(
                        g => new GroupResult
                        {
                            Key = g.Key,
                            Count = g.Count(),
                            Items = g,
                            SubGroups =
                                    g.GroupByMany(
                                        sortFields.Count > 0 ? sortFields.Skip(1).ToList() : sortFields,
                                        nextSelectors)
                        });

                if (sortFields != null && sortFields.Count > 0)
                {
                    var sortKey = sortFields.First();
                    if (sortKey.PropertyName != null) // && sortKey.Index == 0)
                    {
                        if (sortKey.Direction == ListSortDirection.Ascending)
                        {
                            groupBy = groupBy.OrderBy(g => g.Key);
                        }
                        else
                        {
                            groupBy = groupBy.OrderByDescending(g => g.Key);
                        }
                    }
                }

                return groupBy;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            params Func<TElement, object>[] groupSelectors)
        {
            if (groupSelectors != null && groupSelectors.Length > 0)
            {
                var selector = groupSelectors.First();
                var nextSelectors = groupSelectors.Skip(1).ToArray();

                return
                   elements.GroupBy(selector).Select(
                       g => new GroupResult
                       {
                           Key = g.Key,
                           Count = g.Count(),
                           Items = g,
                           SubGroups = g.GroupByMany(nextSelectors)
                       });
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<GroupResult> GroupByMany<TElement>(
            this IEnumerable<TElement> elements,
            IEnumerable<Func<TElement, object>> groupSelectors)
        {
            return GroupByMany<TElement>(elements, groupSelectors.ToArray());
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, IEnumerable<string> properties)
        {
            return GroupByMany(source, properties.ToArray());
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, Type sourceType,
                                                           params string[] properties)
        {
            return source.GroupByMany(null, sourceType, properties);
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, Dictionary<string, string> formatColl,
                                                           Type sourceType, params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (properties != null && properties.Length == 0)
            {
                return null;
            }

            string format = string.Empty;
            var lambdas = new List<LambdaExpression>();
            foreach (var property in properties ?? Array.Empty<string>())
            {
                format = string.Empty;
                var param = Expression.Parameter(source.ElementType, sourceType?.Name);
                // Code to convert complex property to simple property
                var propertyExp = param.GetValueExpression(property, sourceType);
                var conv = Expression.Convert(propertyExp, typeof(object));
                if (formatColl != null)
                {
                    if (formatColl.ContainsKey(property))
                    {
                        format = formatColl.Where(key => key.Key == property).ToList().FirstOrDefault().Value;
                        if (format.Contains(property, StringComparison.Ordinal))
                        {
                            format = format.Replace(property, "0", StringComparison.Ordinal);
                        }

                        if (!string.IsNullOrEmpty(format))
                        {
                            format = System.Text.RegularExpressions.Regex.Match(format, @"{0:(.*?)}").Groups[1].Value;
                        }
                    }

                    if (!string.IsNullOrEmpty(format) && propertyExp.Type != typeof(string))
                    {
                        var formatMethodCall = GetFormatMethodCallExpression(propertyExp, format);
                        conv = Expression.Convert(formatMethodCall, typeof(object));
                    }
                }

                var lambdaExp = Expression.Lambda(conv, new ParameterExpression[] { param });
                lambdas.Add(lambdaExp);
            }

            var values = CreateGeneric(typeof(List<>), lambdas[0].Type);
            foreach (var lambdaExp in lambdas)
            {
                values.Add(lambdaExp.Compile());
            }

            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            var methodInfo = GetGroupByManyMethod();

            // typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var genericArgs = new Type[] { source.ElementType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { source, values }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, Type sourceType,
                                                           List<SortDescription> sortFields, params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (properties != null && properties.Length == 0)
            {
                return null;
            }

            var lambdas = new List<LambdaExpression>();
            foreach (var property in properties ?? Array.Empty<string>())
            {
                var param = Expression.Parameter(source.ElementType, sourceType?.Name);
                // Code to convert complex property to simple property
                var propertyExp = param.GetValueExpression(property, sourceType);
                var conv = Expression.Convert(propertyExp, typeof(object));
                var lambdaExp = Expression.Lambda(conv, new ParameterExpression[] { param });
                lambdas.Add(lambdaExp);
            }

            var values = CreateGeneric(typeof(List<>), lambdas[0].Type);
            foreach (var lambdaExp in lambdas)
            {
                values.Add(lambdaExp.Compile());
            }

            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            var methodInfo = GetGroupByManyMethod2();

            // typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var genericArgs = new Type[] { source.ElementType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result =
                genericMethodInfo.Invoke(null, new object[] { source, sortFields, values }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this IEnumerable source, Type sourceType,
                                                           Func<string, Expression> GetExpressionFunc,
                                                           params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (properties != null && properties.Length == 0)
            {
                return null;
            }

            var elementType = source.GetElementType();
            var lambdas = new List<LambdaExpression>();
            foreach (var property in properties ?? Array.Empty<string>())
            {
                var param = Expression.Parameter(elementType, sourceType?.Name);
                // Code to convert complex property to simple property
                var expressionFunc = GetExpressionFunc != null ? GetExpressionFunc(property) : null;

                if (expressionFunc == null)
                {
                    if (property.Contains('.', StringComparison.Ordinal))
                    {
                        var lambdaExp = GetLambdaWithComplexPropertyNullCheck(source, property, param, sourceType);
                        lambdas.Add(lambdaExp);
                    }
                    else
                    {
                        var propertyExp = param.GetValueExpression(property, sourceType);
                        var conv = Expression.Convert(propertyExp, typeof(object));
                        var lambdaExp = Expression.Lambda(conv, new ParameterExpression[] { param });
                        lambdas.Add(lambdaExp);
                    }
                }
                else
                {
                    var cExp = Expression.Constant(property);
                    var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, param });
                    var lambdaExp = Expression.Lambda(iExp, param);
                    lambdas.Add(lambdaExp);
                }
            }

            var values = CreateGeneric(typeof(List<>), lambdas[0].Type);
            foreach (var lambdaExp in lambdas)
            {
                values.Add(lambdaExp.Compile());
            }

            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            var methodInfo = GetGroupByManyMethod();

            // typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var genericArgs = new Type[] { elementType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { source, values }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this IEnumerable source, Type sourceType,
                                                           List<SortDescription> sortFields,
                                                           Dictionary<string, IComparer<object>> sortComparers,
                                                           Func<string, Expression> GetExpressionFunc,
                                                           params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (properties != null && properties.Length == 0)
            {
                return null;
            }

            var elementType = source.GetElementType();
            var lambdas = new List<LambdaExpression>();
            foreach (var property in properties ?? Array.Empty<string>())
            {
                var param = Expression.Parameter(elementType, sourceType?.Name);
                // Code to convert complex property to simple property
                var expressionFunc = GetExpressionFunc != null ? GetExpressionFunc(property) : null;
                if (expressionFunc == null)
                {
                    var propertyExp = param.GetValueExpression(property, sourceType);
                    var conv = Expression.Convert(propertyExp, typeof(object));
                    var lambdaExp = Expression.Lambda(conv, new ParameterExpression[] { param });
                    lambdas.Add(lambdaExp);
                }
                else
                {
                    var cExp = Expression.Constant(property);
                    var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, param });
                    var lambdaExp = Expression.Lambda(iExp, param);
                    lambdas.Add(lambdaExp);
                }
            }

            var values = CreateGeneric(typeof(List<>), lambdas[0].Type);
            foreach (var lambdaExp in lambdas)
            {
                values.Add(lambdaExp.Compile());
            }

            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            var methodInfo = GetGroupByManyMethod3();

            // typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var genericArgs = new Type[] { elementType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result =
                genericMethodInfo.Invoke(null, new object[] { source, sortFields, sortComparers, properties, values }) as
                IEnumerable<GroupResult>;
            return result;

            // foreach (var groupResult in result)
            // {
            //    yield return groupResult;
            // }
        }

        public static IEnumerable<GroupResult> GroupByMany(this IEnumerable source, Type sourceType,
                                                           List<SortDescription> sortFields,
                                                           Func<string, Expression> GetExpressionFunc,
                                                           params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            if (properties != null && properties.Length == 0)
            {
                return null;
            }

            var elementType = source.GetElementType();
            var lambdas = new List<LambdaExpression>();
            foreach (var property in properties ?? Array.Empty<string>())
            {
                var param = Expression.Parameter(elementType, sourceType?.Name);
                // Code to convert complex property to simple property
                var expressionFunc = GetExpressionFunc != null ? GetExpressionFunc(property) : null;
                if (expressionFunc == null)
                {
                    var propertyExp = param.GetValueExpression(property, sourceType);
                    var conv = Expression.Convert(propertyExp, typeof(object));
                    var lambdaExp = Expression.Lambda(conv, new ParameterExpression[] { param });
                    lambdas.Add(lambdaExp);
                }
                else
                {
                    var cExp = Expression.Constant(property);
                    var iExp = Expression.Invoke(expressionFunc, new Expression[] { cExp, param });
                    var lambdaExp = Expression.Lambda(iExp, param);
                    lambdas.Add(lambdaExp);
                }
            }

            var values = CreateGeneric(typeof(List<>), lambdas[0].Type);
            foreach (var lambdaExp in lambdas)
            {
                values.Add(lambdaExp.Compile());
            }

            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            var methodInfo = GetGroupByManyMethod2();

            // typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var genericArgs = new Type[] { elementType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result =
                genericMethodInfo.Invoke(null, new object[] { source, sortFields, values }) as IEnumerable<GroupResult>;
            return result;

            // foreach (var groupResult in result)
            // {
            //    yield return groupResult;
            // }
        }

        private static MethodInfo GetGroupByManyMethod()
        {
            MethodInfo method = null;
            var methods = typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod);
            foreach (var m in methods)
            {
                var pInfo = m.GetParameters();
                if (pInfo.Length > 1)
                {
                    var p = pInfo[1];
                    if (typeof(IEnumerable<>).Name == p.ParameterType.Name)
                    {
                        method = m;
                        break;
                    }
                }

            }

            return method;
        }

        private static MethodInfo GetGroupByManyMethod2()
        {
            MethodInfo method = null;
            var methods =
                typeof(QueryableExtensions).GetMethods()
                                            .Where(
                                                m =>
                                                m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod);
            foreach (var m in methods)
            {
                var pInfo = m.GetParameters();
                if (pInfo.Length > 2)
                {
                    var p = pInfo[2];
                    if (typeof(IEnumerable<>).Name == p.ParameterType.Name)
                    {
                        method = m;
                        break;
                    }
                }
            }

            return method;
        }

        private static MethodInfo GetGroupByManyMethod3()
        {
            MethodInfo method = null;
            var methods =
                typeof(QueryableExtensions).GetMethods()
                                            .Where(
                                                m =>
                                                m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod);
            foreach (var m in methods)
            {
                var pInfo = m.GetParameters();
                if (pInfo.Length > 3)
                {
                    var p = pInfo[2];
                    if (typeof(Dictionary<string, IComparer<object>>).Name == p.ParameterType.Name)
                    {
                        method = m;
                        break;
                    }
                }
            }

            return method;
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.GroupByMany(sourceType, properties);
        }

        public static IEnumerable<GroupResult> GroupByMany(this IQueryable source, Dictionary<string, string> formatcoll,
                                                           params string[] properties)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var sourceType = source.ElementType;
            return source.GroupByMany(formatcoll, sourceType, properties);
        }

#if EJ2_DNX

        public static IEnumerable<GroupResult> GroupByMany(this DataTable source, params string[] properties)
        {
            //return GroupByMany(source, new List<SortDescription>(), properties);
            var enumerable = source.AsEnumerable();
            var sourceType = typeof(DataRow);
            var lambdas = new List<Func<DataRow, object>>();
            foreach (var property in properties)
            {
                // have a local reference of p, otherwise the Func lambda will refer to the last property finished by this iterator (this is by C# design)
                var p = property;
                Func<DataRow, object> functor = r => r.ItemArray[source.Columns.IndexOf(p)];
                lambdas.Add(functor);
            }
            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            //var methodInfo = typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var methodInfo = GetGroupByManyMethod();
            var genericArgs = new Type[] { sourceType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { enumerable, lambdas }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this DataTable source, List<SortDescription> sortFields, params string[] properties)
        {
            var enumerable = source.AsEnumerable();
            var sourceType = typeof(DataRow);
            List<Func<DataRow, object>> lambdas = new List<Func<DataRow, object>>();
            foreach (var property in properties)
            {
                // have a local reference of p, otherwise the Func lambda will refer to the last property finished by this iterator (this is by C# design)
                var p = property;
                Func<DataRow, object> functor = r => r.ItemArray[source.Columns.IndexOf(p)];
                lambdas.Add(functor);
            }
            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            //var methodInfo = typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var methodInfo = GetGroupByManyMethod();
            var genericArgs = new Type[] { sourceType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { enumerable, sortFields, lambdas }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this DataView source, params string[] properties)
        {
            var enumerable = source.Cast<DataRowView>();
            var sourceType = typeof(DataRowView);
            var lambdas = new List<Func<DataRowView, object>>();
            foreach (var property in properties)
            {
                // have a local reference of p, otherwise the Func lambda will refer to the last property finished by this iterator (this is by C# design)
                var p = property;
                Func<DataRowView, object> functor = (r) =>
                {
                    var data = r[source.Table.Columns.IndexOf(p)];
                    return data;
                };
                lambdas.Add(functor);
            }
            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            //var methodInfo = typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var methodInfo = GetGroupByManyMethod();
            var genericArgs = new Type[] { sourceType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { enumerable, lambdas }) as IEnumerable<GroupResult>;
            return result;
        }

        public static IEnumerable<GroupResult> GroupByMany(this DataView source, List<SortDescription> sortFields, params string[] properties)
        {
            //var enumerable = source.AsEnumerable();
            var enumerable = source.Cast<DataRowView>();
            var sourceType = typeof(DataRowView);
            var lambdas = new List<Func<DataRowView, object>>();
            foreach (var property in properties)
            {
                // have a local reference of p, otherwise the Func lambda will refer to the last property finished by this iterator (this is by C# design)
                var p = property;
                Func<DataRowView, object> functor = (r) =>
                {
                    var data = r[source.Table.Columns.IndexOf(p)];
                    return data;
                };
                lambdas.Add(functor);
            }
            // ElementAt(1) is the GroupByMany method with IEnumerable<T> properties
            //var methodInfo = typeof(QueryableExtensions).GetMethods().Where(m => m.Name == "GroupByMany" && m.IsStatic && m.IsPublic && m.IsGenericMethod).ElementAt(1);
            var methodInfo = GetGroupByManyMethod2();
            var genericArgs = new Type[] { sourceType };
            var genericMethodInfo = methodInfo.MakeGenericMethod(genericArgs);
            var result = genericMethodInfo.Invoke(null, new object[] { enumerable, sortFields, lambdas }) as IEnumerable<GroupResult>;
            return result;
        }
#endif

        #endregion

        private static IList CreateGeneric(Type generic, Type innerType, params object[] args)
        {
            System.Type specificType = generic.MakeGenericType(new System.Type[] { innerType });
            return (IList)Activator.CreateInstance(specificType, args);
        }

        public static Type GetObjectType(this IQueryable source)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }
            var enumerable = source.ElementType;
            if (enumerable == typeof(object))
            {
                var e = source.GetEnumerator();
                if (e.MoveNext())
                {
                    enumerable = e.Current.GetType();
                }
            }

            return enumerable;
        }

        // #if EJ2_DNX
        //        internal static Type CreateClass(IEnumerable<DynamicProperty> properties)
        // #else
        //        internal static TypeInfo CreateClass(IEnumerable<DynamicProperty> properties)
        // #endif
        //        {
        //            return ClassFactory.Instance.GetDynamicClass(properties);

        // }

        // #if EJ2_DNX
        //        internal static Type CreateClass(params DynamicProperty[] properties)
        // #else
        //        internal static TypeInfo CreateClass(params DynamicProperty[] properties)
        // #endif
        //        {
        //            return ClassFactory.Instance.GetDynamicClass(properties);

        // }

        // private static Expression GenerateNew(IEnumerable<string> properties, ParameterExpression paramExpression)
        //        {
        //            var expressions = new List<Expression>();
        //            var dynamicProperties = new List<DynamicProperty>();
        //            foreach (var property in properties)
        //            {
        //                var exp = Expression.PropertyOrField(paramExpression, property);
        //                expressions.Add(exp);
        //                dynamicProperties.Add(new DynamicProperty(property, exp.Type));
        //            }
        //            var classType = CreateClass(dynamicProperties);
        //            var bindings = new List<MemberBinding>();
        //            for (int i = 0; i < dynamicProperties.Count; i++)
        //            {
        // #if EJ2_DNX
        //                bindings.Add(Expression.Bind(classType.GetProperty(dynamicProperties[i].Name), expressions[i]));
        // #else
        //                bindings.Add(Expression.Bind(classType.GetDeclaredProperty(dynamicProperties[i].Name), expressions[i]));
        // #endif
        //            }
        // #if EJ2_DNX
        //            return Expression.MemberInit(Expression.New(classType), bindings);
        // #else
        //            var cinfo = classType.DeclaredConstructors.ToList();
        //            return Expression.MemberInit(Expression.New(cinfo[0]), bindings);
        // #endif
        //        }
    }

    public class GroupResult
    {
        public object Key { get; set; }

        public int Count { get; set; }

        public IEnumerable Items { get; set; }

        public IEnumerable<GroupResult> SubGroups { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", Key, Count);
        }
    }

    public class SortDescriptionIndex
    {
        public int Index { get; set; }

        public SortDescription SortDescription { get; set; }
    }
}