public class GridStorage(Configuration configuration, Storage storage, CommandContext context) : GridBase
{
    private async Task Load(GridDto grid)
    {
        grid.Clear();
        var folderOrFileNameList = await UtilStorage.List(configuration.ConnectionStringStorage, isRecursive: true);
        var dataRowIndex = 0;
        foreach (var item in folderOrFileNameList)
        {
            grid.AddRow();
            grid.AddCell(new() { CellEnum = GridCellEnum.Field, Text = item.FolderOrFileName, DataRowIndex = dataRowIndex });
            grid.AddCell(new() { CellEnum = GridCellEnum.Control, DataRowIndex = dataRowIndex }, item.FolderOrFileName);
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Delete", Name = "Delete" });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModal, Text = "Rename", Name = "Rename" });
            dataRowIndex++;
        }
        // Button Reload, Save
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonReload });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
        // Create
        grid.AddRow();
        grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom });
        grid.AddControl(new() { ControlEnum = GridControlEnum.Button, Text = "Create Folder", Name = "CreateFolder" });
    }

    public async Task Load(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid)
    {
        if (parentCell == null)
        {
            await Load(grid);
        }
        else
        {
            // Modal Delete
            if (parentControl?.Name == "Delete")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = $"Delete File? ({folderOrFileName})" });
                grid.AddRow();
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
            }
            // Modal Rename
            if (parentControl?.Name == "Rename")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom, Name = "Rename", Text = folderOrFileName });
                grid.AddRow();
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
                grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonSave });
            }
        }
    }

    private async Task Save(GridDto grid, GridRequestDto request)
    {
        // Save Rename
        var cellList = grid.CellList().Where(item => item.TextModified != null).ToList();
        foreach (var item in cellList)
        {
            await UtilStorage.Rename(configuration.ConnectionStringStorage, item.Text!, item.TextModified!);
        }
        // Button Create Folder
        if (request.Control?.ControlEnum == GridControlEnum.Button && request.Control.Name == "CreateFolder")
        {
            var control = grid.ControlModifiedList().SingleOrDefault();
            if (control != null)
            {
                await UtilStorage.Create(configuration.ConnectionStringStorage, control.TextModified!);
            }
        }
    }

    public async Task Save(GridDto grid, GridCellDto? parentCell, GridControlDto? parentControl, GridDto? parentGrid, GridRequestDto request)
    {
        if (parentCell == null)
        {
            await Save(grid, request);
            await Load(grid, parentCell, parentControl, parentGrid);
        }
        else
        {
            // Modal Delete
            if (parentControl?.Name == "Delete")
            {
                var dataRowIndex = parentCell.DataRowIndex!;
                var folderOrFileName = parentGrid!.CellList().Where(item => item.CellEnum == GridCellEnum.Field && dataRowIndex == item.DataRowIndex).Select(item => item.Text).Single();
                await UtilStorage.Delete(configuration.ConnectionStringStorage, folderOrFileName!);
                await Load(parentGrid);
            }
            // Modal Rename
            if (parentControl?.Name == "Rename")
            {
                var control = grid.ControlModifiedList().Where(item => item.Name == "Rename").SingleOrDefault();
                if (control != null)
                {
                    await UtilStorage.Rename(configuration.ConnectionStringStorage, control.Text!, control.TextModified!);
                    await Load(parentGrid!);
                }
            }
        }
    }

    protected override Task<GridConfig> Config()
    {
        var result = new GridConfig()
        {
            ColumnList = [
                // new() { FieldName = "FolderOrFileName", ColumnEnum = GridColumnEnum.Text, IsAllowModify = false },
                // new() { FieldName = "IsFolder", ColumnEnum = GridColumnEnum.Text, IsAllowModify = false },
                new() { FieldName = "Name", ColumnEnum = GridColumnEnum.Text, IsAllowModify = true, FieldNameSortCustom = "NameSort", Sort = 1 },
                new() { FieldName = "DateModified", ColumnEnum = GridColumnEnum.Long, Sort = 2 },
                new() { FieldName = "Size", ColumnEnum = GridColumnEnum.Long, Sort = 3 }
            ],
            FieldNameRowKey = "FolderOrFileName", // Used to delete row
            IsAllowNew = true,
            IsAllowDelete = true,
            IsAllowDeleteConfirm = true,
            IsAllowEditForm = true,
            PageSize = 6,
            DefaultSortList = new([new() { FieldName = "Name"}]),
            // DefaultColumnFilterMulti = new() { IsSelectAll = true, TextList = new(["IsFolder"])}
            IsSelectMulti = true,
            IsSelectMultiPatch = true,
        };
        return Task.FromResult(result);
    }

    private List<GridPatchDto> PatchList(GridRequest2Dto request)
    {
        var result = new List<GridPatchDto>();
        var fileCount = 0;
        var folderCount = 0;
        var folderParentCount = 0;
        var list = request.Grid.StateGet().IsSelectMultiListGet();
        foreach (var rowKey in list)
        {
            if (rowKey?.Length == 0)
            {
                folderParentCount += 1;
                continue;
            }
            if (rowKey?.EndsWith("/") == true)
            {
                folderCount += 1;
                continue;
            }
            fileCount += 1;
        }
        var isDelete = fileCount > 0 && folderCount == 0 && folderParentCount == 0;
        var isCopy = fileCount > 0 && folderCount == 0 && folderParentCount == 0;
        var isRename = (fileCount == 1 || folderCount == 1) && folderParentCount == 0;
        var isPaste = request.Grid.StateGet().CustomList?.Count() > 0;
        var isDownload = fileCount > 0 && folderCount == 0 && folderParentCount == 0;
        result = new List<GridPatchDto>([
            new() { ControlName = "Delete", IsDisabled = !isDelete },
                new() { ControlName = "Copy", IsDisabled = !isCopy },
                new() { ControlName = "Rename", IsDisabled = !isRename },
                new() { ControlName = "Paste", IsDisabled = !isPaste },
                new() { ControlName = "Upload", IsDisabled = false },
                new() { ControlName = "Download", IsDisabled = !isDownload }
        ]);
        return result;
    }

    protected override async Task<List<GridPatchDto>> GridLoad2Patch(GridRequest2Dto request)
    {
        await context.UserAuthAsync();
        var result = new List<GridPatchDto>();
        // Button Download
        if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "Download")
        {
            var fileNameList = request.Grid.StateGet().IsSelectMultiListGet().OfType<string>().ToList(); // Filter null
            var fileNameUrlList = storage.DownloadUrl(fileNameList);
            var fileList = new List<GridFileDto>();
            for (int i = 0; i < fileNameList.Count; i++)
            {
                var fileNameOnly = UtilStorage.FolderOrFileNameOnly(fileNameList[i]);
                var fileUrl = fileNameUrlList[i];
                fileList.Add(new GridFileDto { FileName = fileNameOnly, FileUrl = fileUrl });
            }
            result.Add(new() { ControlName = "Download", FileList = fileList });
        }
        // Button Upload
        if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "Upload" && request.Control.FileList != null)
        {
            var folderName = request.Grid.StateGet().PathGet(1);
            var fileNameList = request.Control.FileList.Select(item => folderName + item.FileName).ToList();
            var fileUrlList = storage.UploadUrl(fileNameList);
            var fileList = new List<GridFileDto>();
            for (int i = 0; i < fileNameList.Count; i++)
            {
                var fileNameOnly = UtilStorage.FolderOrFileNameOnly(fileNameList[i]);
                var fileUrl = fileUrlList[i];
                fileList.Add(new GridFileDto { FileName = fileNameOnly, FileUrl = fileUrl });
            }
            result.Add(new() { ControlName = "Upload", FileList = fileList });
        }
        // Button Copy
        if (request.Control?.ControlEnum == GridControlEnum.ButtonCustom && request.Control?.Name == "Copy")
        {
            var list = request.Grid.StateGet().IsSelectMultiListGet();
            request.Grid.StateGet().CustomList = list;
            result.Add(new() { ControlName = "Paste", IsDisabled = false });
        }
        // Button CheckBox
        if (request.Cell?.CellEnum == GridCellEnum.CheckboxSelectMulti)
        {
            result = PatchList(request);
        }
        return result;
    }

    protected override async Task<List<Dynamic>> GridLoad2(GridRequest2Dto request, string? fieldNameDistinct, GridConfig config, GridConfigEnum configEnum, string? modalName)
    {
        await context.UserAuthAsync();
        // Breadcrumb add Home
        var path = request.Grid.StateGet().PathGet();
        if (path == null)
        {
            request.Grid.StateGet().PathListAdd(new() { Name = "Storage", Icon = new() { ClassName = "i-storage" } }); // Breadcrumb Home (Storage)
        }
        // Load
        path = request.Grid.StateGet().PathGet(1); // Breadcrumb without Home (Storage)
        var folderOrFileNameList = await storage.List(folderName: path, isRecursive: false);
        var result = UtilGridReflection.DynamicFrom(folderOrFileNameList, (dataRowFrom, dataRowTo) =>
        {
            dataRowTo["FolderOrFileName"] = dataRowFrom.FolderOrFileName;
            dataRowTo["Name"] = dataRowFrom.Text;
            dataRowTo["Size"] = dataRowFrom.Size;
            dataRowTo["IsFolder"] = dataRowFrom.IsFolder;
            dataRowTo["NameSort"] = (dataRowFrom.IsFolder ? "0" : "1") + dataRowFrom.Text; // Show folders before files
            if (dataRowFrom.IsFolder)
            {
                dataRowTo.IconSet("Name", "i-folder", isLeft: true);
            }
            else
            {
                dataRowTo.IconSet("Name", "i-file", isLeft: true);
                var name = dataRowFrom.FolderOrFileName.ToLower();
                if (name.EndsWith(".txt"))
                {
                    dataRowTo.IconSet("Name", "i-text", isLeft: true);
                }
                if (name.EndsWith(".xlsx"))
                {
                    dataRowTo.IconSet("Name", "i-excel", isLeft: true);
                }
                if (name.EndsWith(".png") || name.EndsWith(".jpg"))
                {
                    dataRowTo.IconSet("Name", "i-image", isLeft: true);
                }
                if (name.EndsWith(".mp4"))
                {
                    dataRowTo.IconSet("Name", "i-video", isLeft: true);
                }
            }
        });
        result = await UtilGrid.GridLoad2(request, result, null, config, configEnum);
        // Add parent directory entry
        var pathParent = path?.TrimEnd('/').Substring(0, path.TrimEnd('/').LastIndexOf("/") + 1); // Returns null for none and empty for root.
        if (pathParent != null)
        {
            var pathParentDynamic = new Dynamic();
            pathParentDynamic["FolderOrFileName"] = pathParent;
            pathParentDynamic["IsFolder"] = true;
            pathParentDynamic["Name"] = "[..]";
            pathParentDynamic["DateModified"] = null;
            pathParentDynamic["Size"] = null;
            pathParentDynamic.IconSet("Name", "i-folder", isLeft: true);
            result.Insert(0, pathParentDynamic);
        }
        return result;
    }

    protected override async Task GridSave2(GridRequest2Dto request, List<Dynamic> sourceList, GridConfig config)
    {
        await context.UserAuthAsync();
        foreach (var item in sourceList)
        {
            switch (item.DynamicEnum)
            {
                case DynamicEnum.Update:
                    {
                        if (item.ValueModifiedGet<string>("Name", out var value, out var valueModified))
                        {
                            var folderOrFileName = (string)item.RowKey!;
                            var folderOrFileNameOnlyNew = valueModified!;
                            await storage.Rename(folderOrFileName, folderOrFileNameOnlyNew);
                        }
                        break;
                    }
                case DynamicEnum.Insert:
                    {
                        if (item.ValueModifiedGet<string>("Name", out var value, out var valueModified))
                        {
                            var path = request.Grid.StateGet().PathGet(1); // Breadcrumb without Home (Storage)
                            var name = valueModified;
                            var folderOrFileNameNew = path + name;
                            await storage.Upload(folderOrFileNameNew, null);
                        }
                        break;
                    }
                case DynamicEnum.Delete:
                    {
                        var folderOrFileName = (string)item.RowKey!;
                        await storage.Delete(folderOrFileName);
                        break;
                    }
            }
        }
    }

    protected override async Task GridSave2Custom(GridRequest2Dto request, GridButtonCustom? buttonCustomClick, List<FieldCustomSaveDto> fieldCustomSaveList, string? modalName)
    {
        await context.UserAuthAsync();
        // Button Delete
        if (buttonCustomClick?.Control.Name == "Delete")
        {
            var list = request.Grid.StateGet().IsSelectMultiListGet();
            if (list != null)
            {
                foreach (var folderOrFileName in list)
                {
                    await storage.Delete(folderOrFileName ?? "");
                }
            }
        }
        // Button Paste
        if (buttonCustomClick?.Control.Name == "Paste")
        {
            var list = request.Grid.StateGet().CustomList;
            var dest = request.Grid.StateGet().PathGet(1);
            if (list != null)
            {
                foreach (var folderOrFileName in list)
                {
                    var length = await storage.Copy(folderOrFileName ?? "", dest ?? "");
                }
            }
            request.Grid.StateGet().IsSelectMultiList = null;
        }
        // Button Select
        if (buttonCustomClick?.Control.Name == "Select")
        {
            var dataRowIndex = buttonCustomClick.Cell.DataRowIndex.GetValueOrDefault(-1);
            var rowKey = request.Grid.State?.RowKeyList?[dataRowIndex];
            if (rowKey != null)
            {
                var pathSourceList = request.Grid.StateGet().PathList ?? new();
                var index = request.Grid.StateGet().PathModalIndexGet() + 1; // Index of home (Storage) path segment
                var pathDestList = pathSourceList.Take(index + 1).ToList(); // Truncate
                var nameList = rowKey.Split("/").Where(item => item.Length > 0).Reverse().ToList();
                foreach (var name in nameList)
                {
                    pathDestList.Insert(index + 1, new() { Name = name, Icon = new() { ClassName = "i-folder" } });
                }
                request.Grid.StateGet().PathList = pathDestList;
            }
        }
        // Button CreateFolder
        if (modalName == "CreateFolder")
        {
            if (buttonCustomClick != null || fieldCustomSaveList.Count > 0)
            {
                var folderName = fieldCustomSaveList?.FirstOrDefault()?.Control?.TextModified;
                if (folderName != null)
                {
                    await storage.Create(folderName);
                }
            }
        }
        // Folder New
        if (modalName == "FolderNew")
        {
            var folderName = fieldCustomSaveList?.Single().Control?.TextModified;
            if (folderName != null)
            {
                var dest = request.ParentGrid?.StateGet().PathGet(1);
                folderName = UtilStorage.FolderOrFileNameOnly(folderName);
                await storage.Create(dest + folderName);
            }
        }
    }

    protected override void GridRender2(GridRequest2Dto request, List<Dynamic> dataRowList, GridConfig config, string? modalName)
    {
        var grid = request.Grid;
        grid.AddControl(new() { ControlEnum = GridControlEnum.Breadcrumb });
        base.GridRender2(request, dataRowList, config, modalName);

        if (modalName == null || modalName == "Sub")
        {
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonModalCustom, Text = "Create Folder", Name = "CreateFolder" });
        }
        if (modalName == "CreateFolder")
        {
            UtilGrid.RenderGrid2(request, dataRowList, config);
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = "Folder Name" });
            grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Validate" });
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        }

        // Folder select
        if (modalName == null || modalName == "CreateFolder")
        {
            if (request.Grid.RowCellList != null)
            {
                var rowButton = request.Grid.RowCellList.Skip(1).First();
                var patchList = PatchList(request);
                var addControl = (string name, bool isPatch) => rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = name, Name = name, Icon = new() { ClassName = "i-" + name.ToLower() }, IsDisabled = patchList.Single(item => item.ControlName == name).IsDisabled, IsPatch = isPatch });
                addControl("Delete", false);
                addControl("Copy", true);
                addControl("Paste", false);
                addControl("Rename", false);
                addControl("Upload", false).FileEnum = GridFileEnum.Upload;
                addControl("Download", false).FileEnum = GridFileEnum.Download;
                rowButton.AddControl(new() { ControlEnum = GridControlEnum.ButtonModalCustom, Text = "New Folder", Name = "FolderNew", Icon = new() { ClassName = "i-folder" } });
                foreach (var row in request.Grid.RowCellList)
                {
                    var dataRowIndex = row.Last().DataRowIndex;
                    if (dataRowIndex != null)
                    {
                        var dataRow = dataRowList[dataRowIndex.Value];
                        if ((bool?)dataRow.GetValueOrDefault("IsFolder") == true) // New row might not contain key IsFolder
                        {
                            row.AddControl(new() { ControlEnum = GridControlEnum.ButtonCustom, Text = "Select", Name = "Select" });
                        }
                        row.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom, Text = "Hello" + (dataRowIndex + 1) });
                    }
                }
            }
        }

        // Folder New
        if (modalName == "FolderNew")
        {
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.Label, Text = "Folder Name:" });
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.FieldCustom });
            grid.AddRow();
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupOk });
            grid.AddControl(new() { ControlEnum = GridControlEnum.ButtonLookupCancel });
        }
    }
}
