public class StorageGrid(Configuration configuration)
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
            grid.AddCell(new() { CellEnum = GridCellEnum.Control, DataRowIndex = dataRowIndex });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Delete", Name = "Delete" });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Rename", Name = "Rename" });
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
            var dataRowIndex = parentCell.DataRowIndex!;
            var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
            grid.AddControl(new() { ControlEnum = GridControlEnum.LabelCustom, Text = $"Delete File? ({folderOrFileName})" });
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
        }
    }

    private async Task Save(GridDto grid)
    {
        // Rename
        var cellList = grid.CellList().Where(item => item.TextModified != null).ToList();
        foreach (var item in cellList)
        {
            await UtilStorage.Rename(configuration.ConnectionStringStorage, item.Text!, item.TextModified!);
        }
        // Create Folder
        if (grid.State?.CustomButtonClick?.Name == "CreateFolder")
        {
            var fieldCustom = grid.ControlList().Where(item => item.ControlEnum == GridControlEnum.FieldCustom).Single();
            await UtilStorage.Create(configuration.ConnectionStringStorage, fieldCustom.Text!);
        }
    }

    public async Task Save(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            await Save(grid);
            await Load(grid, parentCell, parentControl, parentGrid);
        }
        else
        {
            // Delete
            var dataRowIndex = parentCell.DataRowIndex!;
            var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
            await UtilStorage.Delete(configuration.ConnectionStringStorage, folderOrFileName!);
            await Load(parentGrid);
        }
    }
}
