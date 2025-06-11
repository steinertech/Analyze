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
            dataRowIndex++;
        }
        grid.RowCellList.Add(new());
        grid.RowCellList.Last().Add(new() { CellEnum = GridCellEnum.ButtonCancel });
        grid.RowCellList.Last().Add(new() { CellEnum = GridCellEnum.ButtonSave });
    }

    public async Task Save(GridDto grid)
    {
        var cellList = grid.RowCellList!.SelectMany(item => item).Where(item => item.TextModified != null).ToList();
        foreach (var item in cellList)
        {
            await UtilStorage.Rename(configuration.ConnectionStringStorage, item.Text!, item.TextModified!);
        }
        await Load(grid);
    }
}
