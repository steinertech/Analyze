using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

public class GridArticle2 : GridBase
{
    protected override Task<GridConfig> LoadConfig()
    {
        GridConfig result = new() { ColumnList = new() };
        result.ColumnList = new List<GridColumn>
        {
            new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Number, IsRowKey = true, Sort = 1 },
            new() { FieldName = "Text", ColumnEnum = GridColumnEnum.Text, Sort = 2 },
            new() { FieldName = "Price", ColumnEnum = GridColumnEnum.Number, Sort = 3 },
            new() { FieldName = "Quantity", ColumnEnum = GridColumnEnum.Number, Sort = 4 },
            new() { FieldName = "Date", ColumnEnum = GridColumnEnum.Date, Sort = 5 }
        };
        return Task.FromResult(result);
    }

    protected override async Task<List<Dynamic>> LoadDataRowList(GridDto grid, string? lookupFieldName)
    {
        // Data
        var dataRowList = new List<Dynamic>
        {
            new() { { "Id", 1 }, { "Text", "01 Apple" }, { "Price", 88.20 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
            new() { { "Id", 2 }, { "Text", "02 Banana" }, { "Price", 3.20 }, { "Quantity", 7 }, { "Date", "2025-09-02" } },
            new() { { "Id", 3 }, { "Text", "03 Cherry" }, { "Price", 18.20 }, { "Quantity", 12 }, { "Date", "2025-09-02" } },
            new() { { "Id", 4 }, { "Text", "04 Red" }, { "Price", 2.10 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
            new() { { "Id", 5 }, { "Text", "05 Green" }, { "Price", 2.20 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
            new() { { "Id", 6 }, { "Text", "06 Blue" }, { "Price", 2.90 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
            new() { { "Id", 7 }, { "Text", "07 Hello" }, { "Price", 10.90 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
            new() { { "Id", 8 }, { "Text", "08 World" }, { "Price", 10.90 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
            new() { { "Id", 9 }, { "Text", "08 World" }, { "Price", 12.20 }, { "Quantity", 4 }, { "Date", "2025-09-02" } }
        };
        // Query
        var result = await UtilGrid.LoadDataRowList(dataRowList, grid, lookupFieldName);
        return result;
    }
}

public class GridArticle(CommandContext context, CosmosDb cosmosDb)
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
                    var article = new ArticleDto { Id = Guid.NewGuid().ToString(), Text = item.TextModified, Name = Guid.NewGuid().ToString() };
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
        if (pagination.PageIndex >= pagination.PageCount)
        {
            pagination.PageIndex = pagination.PageCount - 1;
        }
        if (pagination.PageIndex < 0)
        {
            pagination.PageIndex = 0;
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
        grid.AddCell(new() { CellEnum = GridCellEnum.HeaderEmpty, Text = "Command" });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = "Text", TextPlaceholder = "Search" });
        grid.AddCell(new() { CellEnum = GridCellEnum.FilterEmpty });
        var dataRowIndex = 0;
        if (grid.State?.ButtonCustomClick?.Name == "New")
        {
            grid.AddRow();
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, FieldName = "Text", DataRowIndex = dataRowIndex, TextPlaceholder = "New" }, null);
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

    public double? Price { get; set; }
}
