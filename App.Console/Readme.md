# App.Console

## Init
```
dotnet new sln --name App
dotnet new console -n App.Console
```

Add to App.Console.csproj
```
  <PropertyGroup>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishAot>true</PublishAot>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishReadyToRun>true</PublishReadyToRun>
```
## Publish
Create self contained exe file
```
dotnet publish App.Console.csproj
```