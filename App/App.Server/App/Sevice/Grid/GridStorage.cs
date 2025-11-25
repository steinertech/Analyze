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
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Create Folder", Name = "CreateFolder" });
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
                grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = $"Delete File? ({folderOrFileName})" });
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
        if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control.Name == "CreateFolder")
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
        var result = new GridConfig() { ColumnList = [new() { FieldName = "Name", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true }] };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, int pageSize)
    {
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, isRecursive: true);
        var id = 0;
        var result = UtilGridReflection.DynamicFrom(folderOrFileNameList, (dataRowFrom, dataRowTo) =>
        {
            id += 1;
            dataRowTo["Id"] = dataRowFrom.FolderOrFileName; // id.ToString(); // TODO null and use Dynamic.ValueModified
            dataRowTo["Name"] = dataRowFrom.FolderOrFileName;
            dataRowTo["IsFolder"] = dataRowFrom.IsFolder;
            if (!dataRowFrom.IsFolder)
            {
                dataRowTo.IconSet("Name", "i-success", null);
            }
        });
        result = await UtilGrid.GridLoad2(request, result, null, 4);
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, GridConfig config)
    {
        var sourceList = UtilGrid.GridSave2(request, config);
        foreach (var item in sourceList)
        {
            var folderOrFileName = item.RowKey!;
            var folderOrFileNameNew = (string)item["Name"]!;
            await UtilStorage.Rename(configuration.ConnectionStringStorage, folderOrFileName, folderOrFileNameNew);
        }
    }
}
