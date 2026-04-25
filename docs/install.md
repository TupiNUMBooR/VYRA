```ps1
dotnet new sln -n VYRA

dotnet new winforms -n "VYRA.App" -f net8.0
dotnet new classlib -n "VYRA.Core" -f net8.0

dotnet sln add "VYRA.App/VYRA.App.csproj"
dotnet sln add "VYRA.Core/VYRA.Core.csproj"

dotnet add "VYRA.App/VYRA.App.csproj" reference "VYRA.Core/VYRA.Core.csproj"

dotnet build

```
