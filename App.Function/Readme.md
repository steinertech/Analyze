# App.Function

```
dotnet new globaljson # Note has no effect on func new
dotnet new sln --name App
func new --template "HttpTrigger" --name Function -f App.Function --target-framework net9.0
# Rename to App.Function.csproj
```