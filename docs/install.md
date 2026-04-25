```ps1
dotnet new classlib -n "VYRA.Core"

dotnet new wpf -n "VYRA.WPF"
dotnet add VYRA.WPF/VYRA.WPF.csproj package Hardcodet.NotifyIcon.Wpf
dotnet add VYRA.WPF/VYRA.WPF.csproj package System.Drawing.Common

dotnet new sln -n VYRA
dotnet sln add "VYRA.Core/VYRA.Core.csproj"
dotnet sln add "VYRA.WPF/VYRA.WPF.csproj"

dotnet build
```

```ps1
dotnet run --project "VYRA.WPF/VYRA.WPF.csproj"
```
