public class GridStorage(Configuration configuration) : GridBase
{
    private async Task Load(GridDto grid)
    {
        grid.Clear();
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, isRecursive: true);
        var dataRowIndex = 0;
        foreach (var item in folderOrFileNameList)
        {
            grid.AddRow();
            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = item.FolderOrFileName, DataRowIndex = dataRowIndex });
            grid.AddCell(new() { CellEnum = GridCellEnum.Control, DataRowIndex = dataRowIndex }, item.FolderOrFileName);
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Delete", Name = "Delete" });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Rename", Name = "Rename" });
            dataRowIndex++;
        }
        // Button Reload, Save
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonReload });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
        // Create
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom });
        grid.AddControl(new() { ControlEnum = GridControlEnum.Button, Text = "Create Folder", Name = "CreateFolder" });
    }

    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            await Load(grid);
        }
        else
        {
            // Modal Delete
            if (parentControl?.Name == "Delete")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = $"Delete File? ({folderOrFileName})" });
                grid.AddRow();
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
            }
            // Modal Rename
            if (parentControl?.Name == "Rename")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom, Name = "Rename", Text = folderOrFileName });
                grid.AddRow();
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
            }
        }
    }

    private async Task Save(GridDto grid, GridRequestDto request)
    {
        // Save Rename
        var cellList = grid.CellList().Where(item => item.TextModified != null).ToList();
        foreach (var item in cellList)
        {
            await UtilStorage.Rename(configuration.ConnectionStringStorage, item.Text!, item.TextModified!);
        }
        // Button Create Folder
        if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control.Name == "CreateFolder")
        {
            var control = grid.ControlModifiedList().SingleOrDefault();
            if (control != null)
            {
                await UtilStorage.Create(configuration.ConnectionStringStorage, control.TextModified!);
            }
        }
    }

    public async Task Save(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid, GridRequestDto request)
    {
        if (parentCell == null)
        {
            await Save(grid, request);
            await Load(grid, parentCell, parentControl, parentGrid);
        }
        else
        {
            // Modal Delete
            if (parentControl?.Name == "Delete")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                await UtilStorage.Delete(configuration.ConnectionStringStorage, folderOrFileName!);
                await Load(parentGrid);
            }
            // Modal Rename
            if (parentControl?.Name == "Rename")
            {
                var control = grid.ControlModifiedList().Where(item => item.Name == "Rename").SingleOrDefault();
                if (control != null)
                {
                    await UtilStorage.Rename(configuration.ConnectionStringStorage, control.Text!, control.TextModified!);
                    await Load(parentGrid!);
                }
            }
        }
    }

    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig()
        {
            ColumnList = [
                // new() { FieldName = "FolderOrFileName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = false },
                // new() { FieldName = "IsFolder", ColumnEnum = GridColumnEnum.Text, IsAllowModify = false },
                new() { FieldName = "Name", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, FieldNameSortCustom = "NameSort" }
            ],
            FieldNameRowKey = "FolderOrFileName", // Used to delete row
            IsAllowNew = true,
            IsAllowDelete = true,
            IsAllowDeleteConfirm = true,
            IsAllowEditForm = true,
            PageSize = 6,
            DefaultSortList = new([new() { FieldName = "Name"}]),
            // DefaultColumnFilterMulti = new() { IsSelectAll = true, TextList = new(["IsFolder"])}
            IsSelectMulti = true,
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        // Breadcrumb add Home
        var path = request.Grid.StateGet().PathGet();
        if (path == null)
        {
            request.Grid.StateGet().PathListAdd(new() { Name = "Storage", Icon = new() { ClassName = "i-storage" } }); // Breadcrumb Home (Storage)
        }
        // Load
        path = request.Grid.StateGet().PathGet(1); // Breadcrumb without Home (Storage)
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, folderName: path, isRecursive: false);
        var result = UtilGridReflection.DynamicFrom(folderOrFileNameList, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["FolderOrFileName"] = dataRowFrom.FolderOrFileName;
            dataRowTo["Name"] = dataRowFrom.Text;
            dataRowTo["IsFolder"] = dataRowFrom.IsFolder;
            dataRowTo["NameSort"] = (dataRowFrom.IsFolder ? "0" : "1") + dataRowFrom.Text; // Show folders before files
            if (dataRowFrom.IsFolder)
            {
                dataRowTo.IconSet("Name", "i-folder", isLeft: true);
            }
            else
            {
                dataRowTo.IconSet("Name", "i-file", isLeft: true);
                var name = dataRowFrom.FolderOrFileName.ToLower();
                if (name.EndsWith(".txt"))
                {
                    dataRowTo.IconSet("Name", "i-text", isLeft: true);
                }
                if (name.EndsWith(".xlsx"))
                {
                    dataRowTo.IconSet("Name", "i-excel", isLeft: true);
                }
                if (name.EndsWith(".png"))
                {
                    dataRowTo.IconSet("Name", "i-image", isLeft: true);
                }
            }
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        // Add parent directory entry
        var pathParent = path?.TrimEnd('/').Substring(0, path.TrimEnd('/').LastIndexOf("/") + 1); // Returns null for none and empty for root.
        if (pathParent != null)
        {
            var pathParentDynamic = new Dynamic();
            pathParentDynamic["FolderOrFileName"] = pathParent;
            pathParentDynamic["IsFolder"] = true;
            pathParentDynamic["Name"] = "[..]";
            pathParentDynamic.IconSet("Name", "i-folder", isLeft: true);
            result.Insert(0, pathParentDynamic);
        }
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        foreach (var item in sourceList)
        {
            switch (item.DynamicEnum)
            {
                case DynamicEnum.Update:
                    {
                        var folderOrFileName = (string)item["Name"]!;
                        var folderOrFileNameNew = (string)item.ValueModifiedGet("Name")!;
                        await UtilStorage.Rename(configuration.ConnectionStringStorage, folderOrFileName, folderOrFileNameNew);
                        break;
                    }
                case DynamicEnum.Insert:
                    {
                        var path = request.Grid.StateGet().PathGet(1); // Breadcrumb without Home (Storage)
                        var name = (string)item.ValueModifiedGet("Name")!;
                        var folderOrFileNameNew = path + name;
                        await UtilStorage.Upload(configuration.ConnectionStringStorage, folderOrFileNameNew, null);
                        break;
                    }
                case DynamicEnum.Delete:
                    {
                        var folderOrFileName = (string)item.RowKey!;
                        await UtilStorage.Delete(configuration.ConnectionStringStorage, folderOrFileName);
                        break;
                    }
            }
        }
    }

    protected override async Task GridSave2Custom(GridRequest2Dto request, GridButtonCustom? buttonCustomClick, List<FieldCustomSaveDto> fieldCustomSaveList, string? modalName)
    {
        if (buttonCustomClick?.Control.Name == "Select")
        {
            var dataRowIndex = buttonCustomClick.Cell.DataRowIndex.GetValueOrDefault(-1);
            var rowKey = request.Grid.State?.RowKeyList?[dataRowIndex];
            if (rowKey != null)
            {
                var pathSourceList = request.Grid.StateGet().PathList ?? new();
                var index = request.Grid.StateGet().PathModalIndexGet() + 1; // Index of home (Storage) path segment
                var pathDestList = pathSourceList.Take(index + 1).ToList(); // Truncate
                var nameList = rowKey.Split("/").Where(item => item.Length > 0).Reverse().ToList();
                foreach (var name in nameList)
                {
                    pathDestList.Insert(index + 1, new() { Name = name, Icon = new() { ClassName = "i-folder" } });
                }
                request.Grid.StateGet().PathList = pathDestList;
            }
        }
        if (modalName == "CreateFolder")
        {
            if (buttonCustomClick != null || fieldCustomSaveList.Count > 0)
            {
                var folderName = fieldCustomSaveList?.FirstOrDefault()?.Control?.TextModified;
                if (folderName != null)
                {
                    await UtilStorage.Create(configuration.ConnectionStringStorage!, folderName);
                }
            }
        }
    }

    public override void Render2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var grid = request.Grid;
        grid.AddControl(new() { ControlEnum = GridControlEnum.Breadcrumb });
        base.Render2(request, dataRowList, config, modalName);

        if (modalName == null || modalName == "Sub")
        {
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModalCustom, Text = "Create Folder", Name = "CreateFolder" });
        }
        if (modalName == "CreateFolder")
        {
            UtilGrid.RenderGrid2(request, dataRowList, config);
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = "Folder Name" });
            grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Validate" });
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        }

        // Folder select
        if (modalName == null || modalName == "CreateFolder")
        {
            if (request.Grid.RowCellList != null)
            {
                var rowButton = request.Grid.RowCellList.Skip(1).First();
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Delete", Name = "Delete", Icon = new() { ClassName = "i-delete" }, IsDisabled = true });
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Copy", Name = "Copy", Icon = new() { ClassName = "i-copy" } });
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Paste", Name = "Paste", Icon = new() { ClassName = "i-paste" } });
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Rename", Name = "Rename", Icon = new() { ClassName = "i-rename" } });
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Upload", Name = "Upload", Icon = new() { ClassName = "i-upload" } });
                foreach (var row in request.Grid.RowCellList)
                {
                    var dataRowIndex = row.Last().DataRowIndex;
                    if (dataRowIndex != null)
                    {
                        var dataRow = dataRowList[dataRowIndex.Value];
                        if ((bool?)dataRow.GetValueOrDefault("IsFolder") == true) // New row might not contain key IsFolder
                        {
                            row.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Select", Name = "Select" });
                        }
                        row.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom, Text = "Hello" + (dataRowIndex + 1) });
                    }
                }
            }
        }
    }
}
