using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

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
                if (id == null)
                {
                    var article = new ArticleDto { Id = Guid.NewGuid().ToString(), Text = item.TextModified };
                    article = await cosmosDb.InsertAsync(article);
                    grid.State.RowKeyList![item.DataRowIndex.Value] = article.Id;
                }
                else
                {
                    var article = await cosmosDb.SelectByIdAsync<ArticleDto>(id);
                    article!.Text = item.TextModified;
                    await cosmosDb.UpdateAsync(article);
                }
            }
            grid.State.FieldSaveList = null;
        }
        if (grid.State?.ButtonCustomClick?.Name == "Delete")
        {
            var id = grid.State!.RowKeyList![grid.State.ButtonCustomClick.DataRowIndex!.Value]!;
            await cosmosDb.DeleteAsync<ArticleDto>(id);
            context.NotificationAdd("Article deleted", NotificationEnum.Success);
        }
        // Load
        await Load(grid);
    }

    private async Task Load(GridDto grid)
    {
        grid.State ??= new();
        grid.State.Pagination ??= new();
        var pagination = grid.State.Pagination;
        pagination.PageSize ??= 3;
        pagination.PageIndex ??= 0;
        pagination.PageIndexDeltaClick ??= 0;
        var query = cosmosDb.Select<ArticleDto>();
        // Filter
        if (grid.State?.FilterList != null)
        {
            foreach (var filter in grid.State.FilterList)
            {
                query = query.Where($"{filter.FieldName}.ToLower().Contains(@0)", filter.Text.ToLower());
            }
        }
        // PageCount
        var rowCount = (await query.CountAsync()).Resource;
        pagination.PageCount = (int)Math.Ceiling((double)rowCount / (double)pagination.PageSize);
        pagination.PageIndex += pagination.PageIndexDeltaClick;
        if (pagination.PageIndex < 0)
        {
            pagination.PageIndex = 0;
        }
        if (pagination.PageIndex >= pagination.PageCount)
        {
            pagination.PageIndex = pagination.PageCount - 1;
        }
        // Sort
        if (grid.State?.Sort != null)
        {
            if (grid.State.Sort.IsDesc == false)
            {
                query = query.OrderBy($"{grid.State.Sort.FieldName}");
            }
            else
            {
                query = query.OrderBy($"{grid.State.Sort.FieldName} DESC");
            }
        }
        // Pagination
        query = query
            .Skip(pagination.PageIndex.Value * pagination.PageSize.Value)
            .Take(pagination.PageSize.Value);
        var list = await query.ToListAsync();
        // Render
        grid.Clear();
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.None });
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonCustom, Text = "New Article", Name = "New" });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Header, FieldName = "Text", Text = "Text" });
        grid.AddCell(new() { CellEnum = GridCellEnum.Header, Text = "Command" });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = "Text" });
        var dataRowIndex = 0;
        if (grid.State?.ButtonCustomClick?.Name == "New")
        {
            grid.AddRow();
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, FieldName = "Text", DataRowIndex = dataRowIndex });
            dataRowIndex += 1;
        }
        foreach (var item in list)
        {
            grid.AddRow();
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = item.Text, FieldName = "Text", DataRowIndex = dataRowIndex }, item.Id);
            grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonCustom, Text = "Delete", Name = "Delete" }, dataRowIndex);
            dataRowIndex += 1;
        }
        grid.AddRow();
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.Pagination });
        grid.AddRow();
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonReload });
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.ButtonSave });
    }
}

public class ArticleDto : DocumentDto
{
    public string? Text { get; set; }

    public int? Price { get; set; }
}
