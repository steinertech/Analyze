﻿using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

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
                var fileNameList = await UtilStorage.List(configuration.ConnectionStringStorage);
                fileNameList = fileNameList.Where(item => item.IsFolder == false && Path.GetExtension(item.FolderOrFileName).ToLower() == ".xlsx").ToList();
                // FileName
                foreach (var item in fileNameList)
                {
                    var fileNameStorage = item.FolderOrFileName;
                    this.list.Add(fileNameStorage, new());
                    var fileNameLocal = UtilServer.FolderNameAppServer() + "App/Data/Storage/" + fileNameStorage;
                    await UtilStorage.Download(configuration.ConnectionStringStorage, fileNameStorage, fileNameLocal);
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
                grid.PaginationGet().PageIndex = grid.State?.Pagination?.PageIndexClick ?? grid.State?.Pagination?.PageIndex ?? 0;
                grid.PaginationGet().PageSize = 10;
                grid.PaginationGet().PageCount = sheet.Value.Count / grid.PaginationGet().PageSize;
                foreach (var row in sheet.Value)
                {
                    rowCount += 1;
                    if ((rowCount - 1) >= grid.PaginationGet().PageIndex * grid.PaginationGet().PageSize && (rowCount -1) < (grid.PaginationGet().PageIndex + 1) * grid.PaginationGet().PageSize)
                    {
                        grid.AddRow();
                        foreach (var cell in row.Value)
                        {
                            var value = cell.Value;
                            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = value.ToString(), DataRowIndex = rowCount -1 });
                        }
                    }
                }
            }
        }
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.Pagination });
    }
}
