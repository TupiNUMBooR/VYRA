```ps1
dotnet new sln -n VYRA

dotnet new wpf -n "VYRA.WPF"
dotnet new classlib -n "VYRA.Core"

dotnet sln add "VYRA.WPF/VYRA.WPF.csproj"
dotnet sln add "VYRA.Core/VYRA.Core.csproj"

dotnet add "VYRA.WPF/VYRA.WPF.csproj" reference "VYRA.Core/VYRA.Core.csproj"

dotnet build
```
