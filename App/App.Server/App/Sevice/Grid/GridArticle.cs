using Microsoft.Azure.Cosmos.Linq;
using System.Linq.Dynamic.Core;

public class GridArticle2 : GridBase
{
    protected override Task<GridConfig> Config2(GridRequest2Dto request)
    {
        GridConfig result = new() { ColumnList = new() };
        result.ColumnList = new List<GridColumn>
        {
            new() { FieldName = "Id", ColumnEnum = GridColumnEnum.Int, Sort = 1 },
            new() { FieldName = "Text", ColumnEnum = GridColumnEnum.Text, Sort = 2, IsAllowModify = true },
            new() { FieldName = "Price", ColumnEnum = GridColumnEnum.Double, Sort = 3, IsAllowModify = true },
            new() { FieldName = "Quantity", ColumnEnum = GridColumnEnum.Int, Sort = 4, IsAllowModify = true, IsAutocomplete = true },
            new() { FieldName = "Date", ColumnEnum = GridColumnEnum.Date, Sort = 5 }
        };
        result.IsAllowNew = true;
        result.IsAllowDelete = true;
        result.IsAllowDeleteConfirm = true;
        result.IsAllowEditForm = true;
        return Task.FromResult(result);
    }

    private List<Dynamic> dataRowList = new List<Dynamic>
    {
        new () { { "Id", 1 }, { "Text", "01 Apple" }, { "Price", 88.20 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
        new() { { "Id", 2 }, { "Text", "02 Banana" }, { "Price", 3.20 }, { "Quantity", 7 }, { "Date", "2025-09-02" } },
        new() { { "Id", 3 }, { "Text", "03 Cherry" }, { "Price", 18.20 }, { "Quantity", 12 }, { "Date", "2025-09-02" } },
        new() { { "Id", 4 }, { "Text", "04 Red" }, { "Price", 2.10 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
        new() { { "Id", 5 }, { "Text", "05 Green" }, { "Price", 2.20 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
        new() { { "Id", 6 }, { "Text", "06 Blue" }, { "Price", 2.90 }, { "Quantity", 1 }, { "Date", "2025-09-02" } },
        new() { { "Id", 7 }, { "Text", "07 Hello" }, { "Price", 10.90 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
        new() { { "Id", 8 }, { "Text", "08 World" }, { "Price", 10.90 }, { "Quantity", 2 }, { "Date", "2025-09-02" } },
        new() { { "Id", 9 }, { "Text", "08 World" }, { "Price", 12.20 }, { "Quantity", 4 }, { "Date", "2025-09-02" } }
    };

    protected override Task GridSave(GridRequestDto request, GridConfig config)
    {
        var sourceList = UtilGrid.GridSave(request, config);
        var destList = dataRowList;
        foreach (var source in sourceList)
        {
            switch (source.DynamicEnum)
            {
                case DynamicEnum.Update:
                    {
                        var id = config.ConvertFrom("Id", source.RowKey);
                        var index = destList.Select((item, index) => (Value: item, Index: index)).Single(item => object.Equals(item.Value["Id"], id)).Index;
                        foreach (var (fieldName, value) in source)
                        {
                            destList[index][fieldName] = value;
                        }
                    }
                    break;
                case DynamicEnum.Insert:
                    {
                        var dest = Dynamic.Create(config);
                        foreach (var (fieldName, value) in source)
                        {
                            var valueDest = value;
                            dest[fieldName] = valueDest;
                        }
                        dest["Id"] = destList.Select(item => (int)item["Id"]!).DefaultIfEmpty().Max() + 1;
                        destList.Add(dest);
                    }
                    break;
                case DynamicEnum.Delete:
                    {
                        var id = config.ConvertFrom("Id", source.RowKey);
                        var index = destList.Select((item, index) => (Value: item, Index: index)).Single(item => object.Equals(item.Value["Id"], id)).Index;
                        destList.RemoveAt(index);
                    }
                    break;
            }
        }
        return Task.CompletedTask;
    }

    protected override Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        var destList = dataRowList;
        UtilGrid.GridSave2(sourceList, destList, config);
        return Task.CompletedTask;
    }

    protected override async Task<List<Dynamic>> GridLoad(GridRequestDto request, string? fieldNameDistinct, int pageSize)
    {
        // Data
        var dataRowList = this.dataRowList;
        // Apply (filter, sort and pagination)
        var result = await UtilGrid.GridLoad(request, dataRowList, fieldNameDistinct, pageSize);
        return result;
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        // Load
        var dataRowList = this.dataRowList;
        // Apply (filter, sort and pagination)
        var result = await UtilGrid.GridLoad2(request, dataRowList, fieldNameDistinct, config, configEnum);
        return result;
    }
}

public class GridArticle(CommandContext context, CosmosDb cosmosDb)
{
    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid, GridRequestDto request)
    {
        await context.UserAuthAsync();
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
        if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control.Name == "Delete")
        {
            var id = grid.State!.RowKeyList![request.Cell!.DataRowIndex!.Value]!;
            await cosmosDb.DeleteAsync<ArticleDto>(id);
            context.NotificationAdd("Article deleted", NotificationEnum.Success);
        }
        // Load
        await Load(grid, request);
    }

    private async Task Load(GridDto grid, GridRequestDto request)
    {
        grid.State ??= new();
        grid.State.Pagination ??= new();
        var pagination = grid.State.Pagination;
        var pageSize = 3; // pagination.PageSize ??= 3;
        pagination.PageIndex ??= 0;
        pagination.PageIndexDeltaClick ??= 0;
        var query = cosmosDb.Select<ArticleDto>();
        // Filter
        if (grid.State?.FilterList != null)
        {
            foreach (var (fieldName, text) in grid.State.FilterList)
            {
                query = query.Where($"{fieldName}.ToLower().Contains(@0)", text.ToLower());
            }
        }
        // PageCount
        var rowCount = (await query.CountAsync()).Resource;
        pagination.PageCount = (int)Math.Ceiling((double)rowCount / (double)3);
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
        var sort = grid.State?.SortList?.FirstOrDefault();
        if (sort != null)
        {
            if (sort.IsDesc == false)
            {
                query = query.OrderBy($"{sort.FieldName}");
            }
            else
            {
                query = query.OrderBy($"{sort.FieldName} DESC");
            }
        }
        // Pagination
        query = query
            .Skip(pagination.PageIndex.Value * pageSize)
            .Take(pageSize);
        var list = await query.ToListAsync();
        // Render
        grid.Clear();
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.None });
        grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.Button, Text = "New Article", Name = "New" });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Header, FieldName = "Text", Text = "Text" });
        grid.AddCell(new() { CellEnum = GridCellEnum.HeaderEmpty, Text = "Command" });
        grid.AddRow();
        grid.AddCell(new() { CellEnum = GridCellEnum.Filter, FieldName = "Text", TextPlaceholder = "Search" });
        grid.AddCell(new() { CellEnum = GridCellEnum.FilterEmpty });
        var dataRowIndex = 0;
        if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control.Name == "New")
        {
            grid.AddRow();
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, FieldName = "Text", DataRowIndex = dataRowIndex, TextPlaceholder = "New" }, null);
            dataRowIndex += 1;
        }
        foreach (var item in list)
        {
            grid.AddRow();
            grid.AddCell(new GridCellDto { CellEnum = GridCellEnum.Field, Text = item.Text, FieldName = "Text", DataRowIndex = dataRowIndex }, item.Id);
            grid.AddControl(new GridControlDto { ControlEnum = GridControlEnum.Button, Text = "Delete", Name = "Delete" }, dataRowIndex);
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
