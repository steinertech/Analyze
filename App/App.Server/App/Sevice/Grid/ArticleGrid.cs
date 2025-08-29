public class ArticleGrid(CommandContext context, CosmosDb cosmosDb)
{
    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        await context.UserAuthenticateAsync();
        // Save
        if (grid.State?.FieldSaveList?.Count() > 0)
        {
            foreach (var item in grid.State.FieldSaveList)
            {
                var id = grid.State.RowKeyList![item.DataRowIndex!.Value];
                var article = await cosmosDb.SelectByIdAsync<ArticleDto>(id);
                article!.Text = item.TextModified;
                await cosmosDb.UpdateAsync(article);
            }
        }
        // Load
        await Load(grid);
    }

    private async Task Load(GridDto grid)
    {
        await context.UserAuthenticateAsync();
        var list = await cosmosDb.Select<ArticleDto>().ToListAsync();
        list = list.ToList();
        // Render
        grid.Clear();
        var dataRowIndex = 0;
        foreach (var item in list)
        {
            grid.AddRow();
            // grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.LabelCustom, Text = item.Text });
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = item.Text, FieldName = "Text", DataRowIndex = dataRowIndex }, item.Id);
            dataRowIndex += 1;
        }
        grid.AddRow();
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonReload });
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonSave });
    }
}