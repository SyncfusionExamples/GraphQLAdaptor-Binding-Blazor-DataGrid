# GraphQL Adaptor Binding Blazor DataGrid

## Overview

This repository demonstrates how to bind a Syncfusion [Blazor DataGrid](https://www.syncfusion.com/blazor-components/blazor-datagrid) to a GraphQL data source using the DataManager GraphQL adaptor. The solution consists of two applications: an ASP.NET Core GraphQL server and a Blazor Server application that consumes GraphQL data through the Grid. The sample shows how DataManager communicates with a GraphQL endpoint and enables Grid data operations by sending GraphQL queries and request arguments to the server. This approach allows the Grid to retrieve only the required data while supporting server-side processing through GraphQL.

## Key Features

- Uses a dedicated GraphQL server hosted in the `ASPNetCoreGraphQlServer-EmptyApp/ASPNetCoreGraphQlServer` project.
- Uses a Blazor application hosted in the `Blazor Server App/BlazorApplication` project.
- Demonstrates binding Syncfusion Blazor DataGrid data through the DataManager GraphQL adaptor.
- Shows how Grid requests are sent to a GraphQL endpoint instead of a traditional REST service.
- Supports GraphQL-based data operations including paging, sorting, filtering, and CRUD operations as described by the sample implementation.
- Demonstrates communication between the GraphQL server application and the Blazor Grid application.
- Can be tested with the Banana Cake Pop GraphQL IDE or other GraphQL-compatible tools.

## Prerequisites

- Visual Studio 2022 or Visual Studio Code
- .NET SDK compatible with the project's target framework

## How to Run the Project

**Visual Studio 2022**

1. Clone or download this repository.
2. Open the solution in `ASPNetCoreGraphQlServer-EmptyApp/ASPNetCoreGraphQlServer`.
3. Restore all NuGet packages.
4. Build and run the GraphQL server application.
5. Open the solution in `Blazor Server App/BlazorApplication`.
6. Restore all NuGet packages.
7. Set `BlazorApplication` as the startup project if required.
8. Build the solution.
9. Run the application using `Ctrl+F5`.

**Visual Studio Code**

1. Open the repository folder in Visual Studio Code.
2. Open the integrated terminal.
3. Navigate to the GraphQL server project directory and run the server.

```bash
cd "ASPNetCoreGraphQlServer-EmptyApp/ASPNetCoreGraphQlServer"
dotnet restore
dotnet run
```

4. Open another terminal and navigate to the Blazor application directory.

```bash
cd "Blazor Server App/BlazorApplication"
dotnet restore
dotnet run
```

5. Open the application URL shown in the console after startup.
6. Verify that the GraphQL server is running before loading the Blazor application.

## Project Structure

- `Blazor Server App/BlazorApplication/Pages/` — contains the page that binds Grid data using the DataManager GraphQL adaptor. 
- `ASPNetCoreGraphQlServer-EmptyApp/ASPNetCoreGraphQlServer/` — contains GraphQL schema, resolvers, and server-side processing logic.

## Support and Feedback

- For general product questions, visit the [Syncfusion Community Forum](https://www.syncfusion.com/forums) or [Syncfusion Support](https://www.syncfusion.com/support).
- To report an issue specific to this sample, open a GitHub issue in this repository.
- For official documentation related to this feature, see https://help.syncfusion.com/grid-sdk/blazor/data-grid/connecting-to-adaptors/graphql-adaptor

## License

This is a Syncfusion sample project provided to demonstrate product usage. Review the [Syncfusion license terms](https://www.syncfusion.com/sales/pricing?category=ui-components) before using Syncfusion components in your own applications.
