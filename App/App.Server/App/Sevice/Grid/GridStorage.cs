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
                new() { FieldName = "IsFolder", ColumnEnum = GridColumnEnum.Text, IsAllowModify = false },
                new() { FieldName = "Name", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true }
            ],
            FieldNameRowKey = "Name", // Used to delete row
            IsAllowNew = true,
            IsAllowDelete = true,
            IsAllowDeleteConfirm = true,
            IsAllowEditForm = true,
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, int pageSize, string? modalName)
    {
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, isRecursive: true);
        var path = request.Grid.StateGet().PathGet();
        folderOrFileNameList = folderOrFileNameList.Where(item => item.FolderOrFileName.StartsWith(path ?? "")).ToList();
        var result = UtilGridReflection.DynamicFrom(folderOrFileNameList, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["Name"] = dataRowFrom.FolderOrFileName;
            dataRowTo["IsFolder"] = dataRowFrom.IsFolder;
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
        result = await UtilGrid.GridLoad2(request, result, null, 4);
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
                        var folderOrFileNameNew = (string)item.ValueModifiedGet("Name")!;
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
                var pathList = request.Grid.StateGet().PathList ?? new();
                var index = request.Grid.StateGet().PathModalIndexGet();
                pathList = pathList.Take(index + 1).ToList(); // Truncate
                var nameList = rowKey.Split("/").Reverse().ToList();
                foreach (var name in nameList)
                {
                    pathList.Insert(index + 1, new() { Name = name });
                }
                request.Grid.StateGet().PathList = pathList;
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
        base.Render2(request, dataRowList, config, modalName);

        var grid = request.Grid;
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
        if (request.Grid.RowCellList != null)
        {
            foreach (var row in request.Grid.RowCellList)
            {
                var dataRowIndex = row.Last().DataRowIndex;
                if (dataRowIndex != null)
                {
                    var dataRow = dataRowList[dataRowIndex.Value];
                    if ((bool?)dataRow["IsFolder"] == true)
                    {
                        row.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Select", Name = "Select" });
                    }
                    row.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom, Text = "Hello" + (dataRowIndex + 1) });
                }
            }
        }
    }
}
