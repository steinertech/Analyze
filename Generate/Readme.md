# Generate
Tamplate for .NET source code generator.
```
dotnet new globaljson
dotnet new sln --name App
dotnet new classlib -n App.Common
dotnet new classlib -f netstandard2.1 --langVersion latest -n App.Generate
md App.Function
cd App.Function
func new --template "HttpTrigger" --name Function -f App.Function # Install https://github.com/Azure/azure-functions-core-tools # dotnet-isolated
```