using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public class GridExcel(Configuration configuration)
{
    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// (FileName, SheetName, RowIndex, ColName, CellValue)
    /// </summary>
    private Dictionary<string, Dictionary<string, Dictionary<uint, Dynamic>>> list = new Dictionary<string, Dictionary<string, Dictionary<uint, Dynamic>>>();

    private bool isInit = false;

    private async Task Init()
    {
        await semaphore.WaitAsync();
        try
        {
            if (!isInit)
            {
                isInit = true;
                var fileNameList = await UtilStorage.List(configuration.ConnectionStringStorage);
                fileNameList = fileNameList.Where(item => item.IsFolder == false && Path.GetExtension(item.FolderOrFileName).ToLower() == ".xlsx").ToList();
                // FileName
                foreach (var item in fileNameList)
                {
                    var fileNameStorage = item.FolderOrFileName;
                    this.list.Add(fileNameStorage, new());
                    var fileNameLocal = UtilServer.FolderNameAppServer() + "App/Data/Storage/" + fileNameStorage;
                    await UtilStorage.DownloadLocal(configuration.ConnectionStringStorage, fileNameStorage, fileNameLocal);
                    using var document = SpreadsheetDocument.Open(fileNameLocal, isEditable: false);
                    var listLocal = UtilOpenXml.List(document.WorkbookPart);
                    var textList = UtilOpenXml.ExcelSharedStringTableGet(document);
                    // SheetName
                    foreach (var sheetName in UtilOpenXml.ExcelSheetNameList(document))
                    {
                        list[fileNameStorage].Add(sheetName, new());
                        var worksheet = UtilOpenXml.ExcelWorksheet(document, sheetName);
                        var rowList = listLocal.OfType<Row>().Where(item => item.Parent?.Parent == worksheet).ToList();
                        // Row
                        foreach (var row in rowList)
                        {
                            uint rowIndex = row.RowIndex!.Value;
                            list[fileNameStorage][sheetName].Add(rowIndex, new());
                            var cellList = row.OfType<Cell>();
                            // Cell
                            foreach (var cell in cellList)
                            {
                                string cellReference = cell.CellReference!;
                                UtilServer.Assert(cellReference.EndsWith(rowIndex.ToString()));
                                string colName = cellReference.Substring(0, rowIndex.ToString().Length);
                                var cellValue = UtilOpenXml.ExcelCellValueGet(cell, textList);
                                list[fileNameStorage][sheetName][rowIndex].Add(colName, cellValue);
                            }
                        }

                    }
                    File.Delete(fileNameLocal);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task Load(GridDto grid)
    {
        await Init();
        // Filter master detail
        var listLocal = list;
        var rowKeyMaster = grid.State?.RowKeyMasterList?.Values?.FirstOrDefault();
        if (rowKeyMaster != null)
        {
            listLocal = listLocal.Where(item => item.Key == rowKeyMaster).ToDictionary();
        }

        grid.Clear();
        foreach (var file in listLocal)
        {
            foreach (var sheet in file.Value)
            {
                var rowCount = 0;
                grid.PaginationGet().PageIndex = grid.State?.Pagination?.PageIndexDeltaClick ?? grid.State?.Pagination?.PageIndex ?? 0;
                var pageSize = 10; // grid.PaginationGet().PageSize = 10;
                grid.PaginationGet().PageCount = sheet.Value.Count / pageSize;
                foreach (var row in sheet.Value)
                {
                    rowCount += 1;
                    if ((rowCount - 1) >= grid.PaginationGet().PageIndex * pageSize && (rowCount -1) < (grid.PaginationGet().PageIndex + 1) * pageSize)
                    {
                        grid.AddRow();
                        foreach (var cell in row.Value)
                        {
                            var value = cell.Value;
                            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = value?.ToString(), DataRowIndex = rowCount -1 });
                        }
                    }
                }
            }
        }
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
    }
}

public class GridExcel2Cache
{
    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // See also Azure Blob Lease for multiple Azure Function instances

    /// <summary>
    /// (FileName, SheetName, RowIndex, ColName, CellValue)
    /// </summary>
    public Dictionary<string, Dictionary<string, Dictionary<uint, Dynamic>>> List = new Dictionary<string, Dictionary<string, Dictionary<uint, Dynamic>>>();

    public async Task Load(string fileNameStorage, Storage storage)
    {
        await semaphore.WaitAsync();
        try
        {
            string? fileNameLocal = null;
            try
            {
                fileNameLocal = await storage.DownloadLocal(fileNameStorage);
                using var document = SpreadsheetDocument.Open(fileNameLocal, isEditable: false);
                var listLocal = UtilOpenXml.List(document.WorkbookPart);
                var textList = UtilOpenXml.ExcelSharedStringTableGet(document);
                List[fileNameStorage] = new();
                // SheetName
                foreach (var sheetName in UtilOpenXml.ExcelSheetNameList(document))
                {
                    List[fileNameStorage].Add(sheetName, new());
                    var worksheet = UtilOpenXml.ExcelWorksheet(document, sheetName);
                    var rowList = listLocal.OfType<Row>().Where(item => item.Parent?.Parent == worksheet).ToList();
                    // Row
                    foreach (var row in rowList)
                    {
                        uint rowIndex = row.RowIndex!.Value;
                        List[fileNameStorage][sheetName].Add(rowIndex, new());
                        var cellList = row.OfType<Cell>();
                        // Cell
                        foreach (var cell in cellList)
                        {
                            string cellReference = cell.CellReference!;
                            UtilServer.Assert(cellReference.EndsWith(rowIndex.ToString()));
                            string colName = cellReference.Substring(0, cellReference.Length - rowIndex.ToString().Length);
                            var cellValue = UtilOpenXml.ExcelCellValueGet(cell, textList);
                            cellValue = cellValue.ToString(); // All text
                            List[fileNameStorage][sheetName][rowIndex].Add(colName, cellValue);
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(fileNameLocal))
                {
                    File.Delete(fileNameLocal);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}

public class GridExcel2(CommandContext context, Storage storage, GridExcel2Cache cache) : GridBase
{
    /// <summary>
    /// Returns first sheet.
    /// </summary>
    private async Task<Dictionary<uint, Dynamic>?> Sheet(GridRequest2Dto request)
    {
        Dictionary<uint, Dynamic>? result = null;
        if (request.Grid.State?.RowKeyMasterList?.TryGetValue("Storage", out var rowKeyMaster) == true)
        {
            if (rowKeyMaster?.ToLower().EndsWith(".xlsx") == true)
            {
                var fileNameStorage = rowKeyMaster;
                // Cache Load
                if (!cache.List.ContainsKey(fileNameStorage))
                {
                    await context.UserAuthAsync();
                    await cache.Load(fileNameStorage, storage);
                }
                var sheet = cache.List[fileNameStorage].FirstOrDefault().Value; // First sheet
                result = sheet;
            }
        }
        return result;
    }

    protected override async Task<GridConfig> Config2(GridRequest2Dto request)
    {
        var result = new GridConfig();
        // Config Column
        var columnList = new List<GridColumn>();
        var sheet = await Sheet(request);
        if (sheet != null)
        {
            var colList = sheet.SelectMany(row => row.Value.Keys).Distinct().ToList();
            foreach (var col in colList)
            {
                columnList.Add(new() { ColumnEnum = GridColumnEnum.Text, FieldName = col });
            }
        }
        result.ColumnList = columnList;
        result.PageSize = 10;
        return result;
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        var result = new List<Dynamic>();
        var sheet = await Sheet(request);
        if (sheet != null)
        {
            result = sheet.Select(item => item.Value).ToList();
            result = await UtilGrid.GridLoad2(request, result, fieldNameDistinct, config, configEnum);
        }
        return result;
    }
}