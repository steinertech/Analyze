# App.Function

```
dotnet new globaljson # Has no effect on func new
dotnet new sln --name App
func new --template "HttpTrigger" --name Function -f App.Function --target-framework net9.0
# Rename to App.Function.csproj
```

## Publish
```
dotnet publish .\App.Function.csproj
cd bin/Release/net9.0/publish
tar -a -c -f ../publish.zip *
cd ../../../..
az functionapp deployment source config-zip --resource-group stc001-prod --name stc001appFunction --src bin\Release\net9.0\publish.zip
```
