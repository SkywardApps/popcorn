# SQLite Migrations

When using the SQLite datbase with Entity framework the Package Management Console canot be used to manage the database schema and data.

Attempting to run Add-Migration or Update-Database will result in the follow error message:

Could not load file or assembly 'Microsoft.EntityFrameworkCore.Design, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.


The issue and work around are documented here:

https://github.com/aspnet/EntityFrameworkCore/issues/7838

and here:

https://docs.microsoft.com/en-us/ef/core/miscellaneous/cli/dotnet


Generally, the solution is as follows:

Add the following to the *.csproj file:
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="2.0.0" PrivateAssets="All" />
  </ItemGroup>
  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.EntityFrameworkCore.Tools.DotNet" Version="2.0.0" />
  </ItemGroup>

Make sure that the version specified in these entries matches the versions required to support the version of .net you are using.

From the Developer Command Prompt for Visual Studio 2xxx run the following commands:

+ dotnet add package Microsoft.EntityFrameworkCore.Design

+ dotnet restore

Now the command:

+ dotnet ef migrations add [NAME]' 

This will create a migration of the name provided in the Migrations folder.

Finally, the database must be updated and the migration applied:

+ dotnet ef database update
