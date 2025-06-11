using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Extensions.Configuration;

public class ExcelGrid(Configuration configuration)
{
    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// (FileName, SheetName, RowIndex, ColName, CellValue)
    /// </summary>
    private Dictionary<string, Dictionary<string, Dictionary<uint, Dictionary<string, object>>>> list = new Dictionary<string, Dictionary<string, Dictionary<uint, Dictionary<string, object>>>>();

    private bool isInit = false;

    private async Task Init()
    {
        await semaphore.WaitAsync();
        try
        {
            if (!isInit)
            {
                isInit = true;
                var fileNameList = await UtilStorage.FileOrFolderNameList(configuration.ConnectionStringStorage);
                fileNameList = fileNameList.Where(item => Path.GetExtension(item).ToLower() == ".xlsx").ToList();
                // FileName
                foreach (var fileNameStorage in fileNameList)
                {
                    this.list.Add(fileNameStorage, new());
                    var fileNameLocal = UtilServer.FolderNameAppServer() + "Data/Storage/" + fileNameStorage;
                    await UtilStorage.Download(fileNameStorage, fileNameLocal, configuration.ConnectionStringStorage);
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

        grid.RowCellList = new();
        foreach (var file in list)
        {
            foreach (var sheet in file.Value)
            {
                var rowCount = 0;
                foreach (var row in sheet.Value)
                {
                    rowCount += 1;
                    if (rowCount > 10)
                    {
                        break;
                    }
                    grid.RowCellList.Add(new());
                    foreach (var cell in row.Value)
                    {
                        var value = cell.Value;
                        grid.RowCellList.Last().Add(new GridCellDto { CellEnum = GridCellEnum.Field, Text = value.ToString() });
                    }
                }
            }
        }
    }
}
