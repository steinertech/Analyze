public class ArticleGrid(CommandContext context, CosmosDb cosmosDb)
{
    public async Task Load(GridDto grid)
    {
        await context.UserAuthenticate();
        var list = await cosmosDb.Select<ArticleDto>().ToListAsync();
        foreach (var item in list)
        {
            grid.AddRow();
            grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.LabelCustom, Text = item.Text });
            // grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = item.Text });
        }
    }
}