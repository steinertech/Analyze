# App
Start DevContainer Codespace
* App.Web (**Port 4200**)
* App.Server configuration manage user secrets
* App.Server (**Port 7138**) - Make Public

# CosmosDb Consumption
* Serverless
* Metrics, Total Request Units, Last 30 days
* Data Explorer, Setting, RU Limit

# ToDo
* GridRequest2GridEnum see also
* GridArg
* Excel hours and hours total
* Validate cell
* Link to navigate to another grid
* Pagination [Done]
* Master detail [Done]
* Column width [Done]
* Colspan, Rowspan
* Label
* Storage lease
* Column order
* Column lookup [Done]
* Timesheet component
* Calendar component
* Storage component [Done]
* Excel sheets component master detail [Done]
* Check App.Web and App.Server version on every request. [Done]
* Move folder Data/ [Done]
* Load and save to load command only [Done]
* Audit, History
* Css svg content [Done]
* Class for left and right icon symbol [Done]
* Report html to pdf
* GridDebug (without css classes); tailwind @apply component
* Tenant, Session [Done]
* DomainName (my.localhost) [Done]
* Multi Language [Done]
* Notification [Done]
* Notification language
* Notification parameter

# Misc
* https://www.browserling.com
* https://github.com/mui/mui-x

# Code
```
public class GridArg
{
    public GridArg(GridRequest2Dto request, List<Dynamic> sourceList, List<Dynamic> dataRowList, GridConfig config, string? modalName, GridControlDto? buttonCustomClick, List<ControlSaveDto> fieldCustomSaveList)
    {
        Request = request;
        SourceList = sourceList;
        DataRowList = dataRowList;
        Config = config;
        ModalName = modalName;
        ButtonCustomClick = buttonCustomClick;
        FieldCustomSaveList = fieldCustomSaveList;
    }

    public GridRequest2Dto Request { get; set; }

    public List<Dynamic> SourceList { get; set; }

    public List<Dynamic> DataRowList { get; set; }

    public GridConfig Config { get; set; }

    public string? ModalName { get; set; }

    public GridControlDto? ButtonCustomClick { get; set; }

    public List<ControlSaveDto> FieldCustomSaveList { get; set; }
}

    public override async Task GridSave2Custom(GridRequest2Dto request, List<ControlSaveDto> fieldSaveList, GridControlDto? buttonClick)
    {
        if (buttonClick != null && fieldSaveList.Count > 0)
        {
            var folderName = fieldSaveList[0].TextModified;
            if (folderName != null)
            {
                await UtilStorage.Create(configuration.ConnectionStringStorage!, folderName);
            }
        }
    }
```