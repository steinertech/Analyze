public class StorageGrid(Configuration configuration)
{
    public async Task Load(GridDto grid)
    {
        grid.RowCellList = new();
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, isRecursive: true);
        var dataRowIndex = 0;
        foreach (var item in folderOrFileNameList)
        {
            grid.RowCellList.Add(new());
            grid.RowCellList.Last().Add(new() { CellEnum = GridCellEnum.Field, Text = item.FolderOrFileName, DataRowIndex = dataRowIndex });
            grid.RowCellList.Last().Add(new()
            {
                CellEnum = GridCellEnum.Control,
                DataRowIndex = dataRowIndex,
                ControlList = [
                new() { ControlEnum = ControlEnum.ButtonCustom, Text = "Delete", Name = "Delete" },
                ]
            });
            dataRowIndex++;
        }
        // Button Cancel, Save
        grid.RowCellList.Add(new());
        grid.RowCellList.Last().Add(new()
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = ControlEnum.ButtonCancel },
            new() { ControlEnum = ControlEnum.ButtonSave },
            ]
        });
        grid.RowCellList.Add(new());
        grid.RowCellList.Last().Add(new()
        {
            CellEnum = GridCellEnum.Control,
            ControlList = [
            new() { ControlEnum = ControlEnum.FieldCustom },
            new() { ControlEnum = ControlEnum.ButtonCustom, Text = "New Folder", Name = "NewFolder" },
            ]
        });
    }

    public async Task Save(GridDto grid)
    {
        // FolderOrFileName rename
        var cellList = grid.RowCellList!.SelectMany(item => item).Where(item => item.TextModified != null).ToList();
        foreach (var item in cellList)
        {
            await UtilStorage.Rename(configuration.ConnectionStringStorage, item.Text!, item.TextModified!);
        }
        // FolderOrFileName delete
        if (grid.State?.CustomButtonClick?.Name == "Delete")
        {
            var dataRowIndex = grid.State.CustomButtonClick.DataRowIndex;
            if (dataRowIndex != null)
            {
                var folderOrFileName = grid.RowCellList!.SelectMany(item => item).Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                await UtilStorage.Delete(configuration.ConnectionStringStorage, folderOrFileName!);
            }
        }
        // New Folder
        if (grid.State?.CustomButtonClick?.Name == "NewFolder")
        {
            var fieldCustom = grid.RowCellList!.SelectMany(item => item).SelectMany(item => item.ControlList ?? []).Where(item => item.ControlEnum == ControlEnum.FieldCustom).Single();
            await UtilStorage.Create(configuration.ConnectionStringStorage, fieldCustom.Text!);
        }
        await Load(grid);
    }
}
