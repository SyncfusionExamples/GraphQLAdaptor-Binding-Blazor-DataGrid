using ASPNetCoreGraphQlServer.Data;
using ASPNetCoreGraphQlServer.Models;
using System.Collections;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ASPNetCoreGraphQlServer.GraphQl
{
    public class GraphQLQuery
    {

        #region OrdersData Resolver
        public ReturnType<Order> OrdersData(DataManagerRequest dataManager)
        {
            IEnumerable<Order> result = Orders;
            if (dataManager.Search != null)
            {
                result = DataOperations.PerformSearching(result, dataManager.Search);
            }
            if (dataManager.Sorted != null)
            {
                result = DataOperations.PerformSorting(result, dataManager.Sorted);
            }
            if (dataManager.Where != null)
            {
                result = DataOperations.PerformFiltering<Order>(result.AsQueryable(), dataManager.Where, dataManager.Where[0].Condition).ToList();
            }
            int count = result.Count();
            if (dataManager.Skip != 0)
            {
                result = DataOperations.PerformSkip(result, dataManager.Skip);
            }
            if (dataManager.Take != 0)
            {
                result = DataOperations.PerformTake(result, dataManager.Take);
            }
            if (dataManager.Aggregates != null)
            {
                IDictionary<string, object> aggregates = DataUtil.PerformAggregation(result, dataManager.Aggregates);
                return new ReturnType<Order>() { Count = count, Result = result, Aggregates = aggregates };
            }
            return dataManager.RequiresCounts ? new ReturnType<Order>() { Result = result, Count = count } : new ReturnType<Order>() { Result = result };
        }

        public static List<Order> Orders { get; set; } = GetOrdersList();

        private static List<Order> GetOrdersList()
        {
            var data = new List<Order>();
            int count = 1000;
            int employeeCount = 0;
            for (int i = 0; i < 10; i++)
            {
                data.Add(new Order() { OrderID = count + 1, EmployeeID = employeeCount + 1,  CustomerID = "ALFKI", OrderDate = new DateTime(2023, 08, 23), Freight = 5.7 * 2, Address = new CustomerAddress() { ShipCity = "Berlin", ShipCountry = "Denmark" }  });
                data.Add(new Order() { OrderID = count + 2, EmployeeID = employeeCount + 2, CustomerID = "ANANTR", OrderDate = new DateTime(1994, 08, 24), Freight = 6.7 * 2, Address = new CustomerAddress() { ShipCity = "Madrid", ShipCountry = "Brazil" } });
                data.Add(new Order() { OrderID = count + 3, EmployeeID = employeeCount + 3, CustomerID = "BLONP", OrderDate = new DateTime(1993, 08, 25), Freight = 7.7 * 2, Address = new CustomerAddress() { ShipCity = "Cholchester", ShipCountry = "Germany" } });
                data.Add(new Order() { OrderID = count + 4, EmployeeID = employeeCount + 4, CustomerID = "ANTON", OrderDate = new DateTime(1992, 08, 26), Freight = 8.7 * 2, Address = new CustomerAddress() { ShipCity = "Marseille", ShipCountry = "Austria" } });
                data.Add(new Order() { OrderID = count + 5, EmployeeID = employeeCount + 5, CustomerID = "BOLID", OrderDate = new DateTime(1991, 08, 27), Freight = 9.7 * 2, Address = new CustomerAddress() { ShipCity = "Tsawassen", ShipCountry = "Switzerland" } });
                count += 5;
                employeeCount += 5;
            }
            return data;
        }
        #endregion


        #region ForeignKey Resolver
        public ReturnType<EmployeeData> EmployeeDataResolver(DataManagerRequest dataManager)
        {
            var dataManagerRequest = dataManager;
            IEnumerable<EmployeeData> result = Employees;
            int count = result.Count();
            if (dataManagerRequest.Skip > 0 || dataManagerRequest.Take > 0)
            {
                result = result.Skip(dataManagerRequest.Skip).Take(dataManagerRequest.Take).ToList();
            }
            return new ReturnType<EmployeeData>() { Count = count, Result = result };
        }
        public static List<EmployeeData> Employees { get; set; } = GetEmployeeList();
        private static List<EmployeeData> GetEmployeeList()
        {
            var employees = new List<EmployeeData>();
            for (int i = 1; i <= 50; i++)
            {
                employees.Add(new EmployeeData
                {
                    EmployeeID = i,
                    FirstName = "Foreign FirstName" + i,
                    LastName = "Foreign LastName" + i
                });
            }
            return employees;
        }
        #endregion

    }


}
