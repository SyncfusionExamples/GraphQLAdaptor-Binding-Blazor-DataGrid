namespace ASPNetCoreGraphQlServer.Models
{
    public class EmployeeData
    {

        [GraphQLName("EmployeeID")]
        public int? EmployeeID { get; set; }

        [GraphQLName("FirstName")]
        public string? FirstName { get; set; }

        [GraphQLName("LastName")]
        public string? LastName { get; set; }
    }
}
