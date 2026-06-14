using System.CommandLine;

var functionNameOption = new Option<string>("--function-name") { Required = true };
var resourceGroupOption = new Option<string>("--resource-group") { Required = true };

var deployCommand = new Command("deploy", "Deploy to Azure Functions")
{
    functionNameOption,
    resourceGroupOption
};
deployCommand.SetAction(parseResult =>
{
    var functionName = parseResult.GetValue(functionNameOption)!;
    var resourceGroup = parseResult.GetValue(resourceGroupOption)!;
    CommandDeploy.Run(functionName, resourceGroup);
});

var rootCommand = new RootCommand("App CLI");
rootCommand.Add(deployCommand);

return rootCommand.Parse(args).Invoke();
