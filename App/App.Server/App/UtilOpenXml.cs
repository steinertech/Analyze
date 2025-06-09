using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

public class UtilOpenXml
{
    public static void List(OpenXmlPart? part, List<object> result, string? pathDebug)
    {
        if (part != null)
        {
            if (pathDebug != null)
            {
                pathDebug += "." + part.GetType().Name;
                Console.WriteLine(pathDebug);
            }
            result.Add(part);
            if (part.Parts != null)
            {
                foreach (var item in part.Parts)
                {
                    List(item.OpenXmlPart, result, pathDebug);
                    var element = item.OpenXmlPart.RootElement!;
                    List(element, result, pathDebug);
                }
            }
        }
    }

    public static List<object> List(OpenXmlPart? part, bool isDebug = false)
    {
        var result = new List<object>();
        List(part, result, isDebug ? "" : null);
        return result;
    }

    public static void List(OpenXmlElement? element, List<object> result, string? pathDebug)
    {
        if (element != null)
        {
            if (pathDebug != null)
            {
                pathDebug += "." + element.GetType().Name;
                if (element is Text text)
                {
                    pathDebug += "(" + element.InnerText + ")";
                }
                if (element is CellValue cellValue)
                {
                    pathDebug += "(" + cellValue.Text + ")";
                }
                Console.WriteLine(pathDebug);
            }
            result.Add(element);
            foreach (var item in element)
            {
                List(item, result, pathDebug);
            }
        }
    }

    public static List<object> List(OpenXmlElement? element, bool isDebug = false)
    {
        var result = new List<object>();
        List(element, result, isDebug ? "" : null);
        return result;
    }

    public static object ExcelCellValueGet(Cell cell, List<string> textList)
    {
        if (cell.DataType! == "s") // SharedStringTable
        {
            var textIndex = int.Parse(cell.CellValue!.Text);
            var resultText = textList[textIndex];
            return resultText;
        }
        if (cell.DataType == null)
        {
            var resultNumber = double.Parse(cell.CellValue!.Text);
            return resultNumber;
        }
        throw new Exception("Cell type unknown!");
    }

    public static (int Row, int Col) ExcelCellReference(string value)
    {
        int rowIndex = 0;
        int colIndex = 0;
        int i = 0;

        // Process column part of the cell reference
        while (i < value.Length && char.IsLetter(value[i]))
        {
            colIndex = colIndex * 26 + (char.ToUpper(value[i]) - 'A' + 1);
            i++;
        }

        // Process row part of the cell reference
        while (i < value.Length && char.IsDigit(value[i]))
        {
            rowIndex = rowIndex * 10 + (value[i] - '0');
            i++;
        }

        return (rowIndex, colIndex);
    }

    public static string ExcelCellReference((int Row, int Col) value)
    {
        string columnReference = string.Empty;
        while (value.Col > 0)
        {
            value.Col--;
            columnReference = (char)('A' + value.Col % 26) + columnReference;
            value.Col /= 26;
        }
        return $"{columnReference}{value.Row}";
    }

    public static List<string> ExcelSharedStringTableGet(SpreadsheetDocument document)
    {
        var result = new List<string>();
        var sharedStringTable = document.WorkbookPart!.SharedStringTablePart!.SharedStringTable!;
        foreach (var item in sharedStringTable.Elements<SharedStringItem>())
        {
            result.Add(item.InnerText);
        }
        return result;
    }

    public static void ExcelSharedStringTableSet(SpreadsheetDocument document, List<string> value)
    {
        var result = new List<SharedStringItem>();
        foreach (var item in value)
        {
            SharedStringItem sharedStringItem = new SharedStringItem();
            Text text = new Text { Text = item };
            sharedStringItem.Append(text);
            result.Add(sharedStringItem);
        }
        var sharedStringTable = new SharedStringTable(result);
        document.WorkbookPart!.SharedStringTablePart!.SharedStringTable = sharedStringTable;
    }

    public static List<string> ExcelSheetNameList(SpreadsheetDocument document)
    {
        var list = List(document.WorkbookPart?.Workbook).ToList(); // Sheets=document.WorkbookPart?.Workbook

        var sheetList = list.OfType<Sheet>();

        var result = sheetList.Select(item => item.Name!.Value!).ToList();

        return result;
    }

    public static Worksheet ExcelWorksheet(SpreadsheetDocument document, string sheetName)
    {
        var list = List(document.WorkbookPart?.Workbook).ToList(); // Sheets=document.WorkbookPart?.Workbook

        var sheet = list.OfType<Sheet>().Where(item => item.Name == sheetName).Single();

        var worksheetPart = (document.WorkbookPart!.GetPartById(sheet.Id!) as WorksheetPart)!;

        var result = worksheetPart.Worksheet;

        return result;
    }
}
