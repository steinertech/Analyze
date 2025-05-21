# App.Function

```
dotnet new globaljson # Has no effect on func new
dotnet new sln --name App
md App.Function
cd App.Function
func new --template "HttpTrigger" --name Function -f App.Function --target-framework net9.0
# Rename to App.Function.csproj
```

```
AuthorizationLevel.Anonymous
```

## Publish
```
dotnet publish ./App.Function.csproj
cd bin/Release/net9.0/publish
tar -a -c -f ../publish.zip *
cd ../../../..
az functionapp deployment source config-zip --resource-group stc001-prod --name stc001appFunction --src bin\Release\net9.0\publish.zip
```

## Publish (SelfContained)
App.Function.csproj
```
<PropertyGroup>
  <SelfContained>true</SelfContained>
  <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
```

```
dotnet publish ./App.Function.csproj
cd bin/Release/net9.0/linux-x64/publish
tar -a -c -f ../publish.zip *
cd ../../../../..
az functionapp deployment source config-zip --resource-group stc001-prod --name stc001appFunction --src bin/Release/net9.0/linux-x64/publish.zip
```

## Publish (ReadyToRun)
App.Function.csproj
```
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
```

```
dotnet publish ./App.Function.csproj
cd bin/Release/net9.0/linux-x64/publish
tar -a -c -f ../publish.zip *
cd ../../../../..
az functionapp deployment source config-zip --resource-group stc001-prod --name stc001appFunction --src bin/Release/net9.0/linux-x64/publish.zip
```
