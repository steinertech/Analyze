public class ArticleGrid(CommandContext context, CosmosDb cosmosDb)
{
    public async Task Load(GridDto grid)
    {
        await context.UserAuthenticate();
        var list = await cosmosDb.Select<ArticleDto>().ToListAsync();
        list = list.Take(2).ToList();
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