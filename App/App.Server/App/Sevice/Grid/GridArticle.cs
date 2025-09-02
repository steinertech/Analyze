using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

public class GridBase
{
    protected virtual Task<List<GridColumnDto>> LoadColumnList()
    {
        var result = new List<GridColumnDto>();
        return Task.FromResult(result);
    }

    protected virtual Task<List<GridHeaderLookupDataRowDto>> LoadHeaderLookup()
    {
        var result = new List<GridHeaderLookupDataRowDto>();
        return Task.FromResult(result);
    }

    protected virtual Task<List<Dictionary<string, object>>> LoadRowList(GridDto grid)
    {
        var result = new List<Dictionary<string, object>>();
        return Task.FromResult(result);
    }

    protected Task Render(GridDto grid, List<Dictionary<string, object>> rowList, List<GridColumnDto> columnList)
    {
        grid.Clear();
        var columnRowKey = columnList.Where(item => item.IsRowKey == true).SingleOrDefault();
        var dataRowIndex = 0;
        foreach (var row in rowList)
        {
            grid.AddRow();
            foreach (var column in columnList.OrderBy(item => item.Sort))
            {
                var text = row[column.FieldName!].ToString();
                if (columnRowKey == null)
                {
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex });
                }
                else
                {
                    var rowKey = row[columnRowKey.FieldName!].ToString();
                    grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = text, FieldName = column.FieldName, DataRowIndex = dataRowIndex }, rowKey);
                }
            }
            dataRowIndex += 1;
        }
        return Task.CompletedTask;
    }

    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        var rowList = await LoadRowList(grid);
        var columnList = await LoadColumnList();
        await Render(grid, rowList, columnList);
    }
}

public class GridArticle2 : GridBase
{
    protected override Task<List<GridColumnDto>> LoadColumnList()
    {
        var result = new List<GridColumnDto>
        {
            new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Number, IsRowKey = true },
            new() { FieldName = "Text", ColumnEnum = GridColumnEnum.Text },
            new() { FieldName = "Price", ColumnEnum = GridColumnEnum.Number },
            new() { FieldName = "Quantity", ColumnEnum = GridColumnEnum.Number },
            new() { FieldName = "Date", ColumnEnum = GridColumnEnum.Date }
        };
        return Task.FromResult(result);
    }

    protected override Task<List<Dictionary<string, object>>> LoadRowList(GridDto grid)
    {
        var result = new List<Dictionary<string, object>>
        {
            new() {
                { "Id", 1 },
                { "Text", "Apple" },
                { "Price", 88.20 },
                { "Quantity", 2 },
                { "Date", "2025-09-02" }
            },
            new() {
                { "Id", 2 },
                { "Text", "Banana" },
                { "Price", 88.20 },
                { "Quantity", 2 },
                { "Date", "2025-09-02" }
            },
        };
        return Task.FromResult(result);
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
