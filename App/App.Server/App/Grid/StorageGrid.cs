public class StorageGrid(Configuration configuration)
{
    public async Task Load(GridDto grid)
    {
        grid.RowCellList = new();
        var folderOrFileNameList = await UtilStorage.FileOrFolderNameList(configuration.ConnectionStringStorage, isRecursive: true);
        var dataRowIndex = 0;
        foreach (var item in folderOrFileNameList)
        {
            grid.RowCellList.Add(new());
            grid.RowCellList.Last().Add(new() { CellEnum = GridCellEnum.Field, Text = item, DataRowIndex = dataRowIndex });
            grid.RowCellList.Last().Add(new()
            {
                CellEnum = GridCellEnum.Control,
                DataRowIndex = dataRowIndex,
                ControlList = [
                new() { ControlEnum = ControlEnum.ButtonCustom, Text = "Delete"},
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
        var dataRowIndexList = grid.RowCellList!.SelectMany(item => item).Where(item => item.ControlList != null && item.ControlList.Where(item => item.IsClick == true).Any()).Select(item => item.DataRowIndex).ToList();
        var folderOrFileNameList = grid.RowCellList!.SelectMany(item => item).Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndexList.Contains(item.DataRowIndex)).Select(item => item.Text).ToList();
        foreach (var item in folderOrFileNameList)
        {
            await UtilStorage.Delete(configuration.ConnectionStringStorage, item!);
        }
        await Load(grid);
    }
}
