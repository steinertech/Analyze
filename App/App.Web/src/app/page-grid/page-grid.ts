import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectionStrategy, Component, effect, ElementRef, HostListener, inject, Input, signal, ViewChild, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GridCellDto, GridCellEnum, GridControlDto, GridControlEnum, GridFileEnum, GridDto, GridRequest2Dto, ServerApi } from '../generate';
import { UtilClient } from '../util-client';

@Component({
  selector: 'app-page-grid',
  imports: [
    CommonModule, // Json pipe
    FormsModule, // Used for ngModels    
  ],
  templateUrl: './page-grid.html',
  styleUrl: './page-grid.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageGrid implements AfterViewInit {
  private serverApi = inject(ServerApi)

  constructor() {
    effect(() => {
      // console.log('Update _grid')
      this._grid = this.grid()
    })
    effect(() => {
      // console.log('Update _grid')
      this._lookup = this.lookup()
    })
  }

  /** Wait for effect to set _grid */
  gridSetWait(grid: GridDto): Promise<GridDto> {
    return new Promise(resolve => {
      const interval = setInterval(() => {
        if (this._grid === grid) {
          clearInterval(interval)
          resolve(grid)
        }
      }, 0);
    })
  }

  /** If this component is a lookup, load data. */
  async ngAfterViewInit() {
    if (this.parent?._lookup) {
      const lookupGrid = this.parent._lookup.grid()
      this.grid.set(lookupGrid)
      await this.gridSetWait(lookupGrid)
      await PageGrid.commandGridLoad2(this, this.parent?._lookup.cell, this.parent._lookup.control)
    }
  }

  static async commandGridLoad2(pageGrid: PageGrid, cell?: GridCellDto, control?: GridControlDto) {
    const grid = pageGrid._grid!
    const parentCell = pageGrid.parent?._lookup?.cell
    const parentControl = pageGrid.parent?._lookup?.control
    const parentGrid = pageGrid.parent?._grid
    const grandParentCell = pageGrid.parent?.parent?._lookup?.cell
    const grandParentControl = pageGrid.parent?.parent?._lookup?.control
    const grandParentGrid = pageGrid.parent?.parent?._grid
    //
    const gridClone = grid ? { ...grid } : undefined
    delete gridClone?.rowCellList
    const cellClone = cell ? { ...cell } : undefined
    delete cellClone?.controlList
    const parentGridClone = parentGrid ? { ...parentGrid } : undefined
    delete parentGridClone?.rowCellList
    const parentCellClone = parentCell ? { ...parentCell } : undefined
    delete parentCellClone?.controlList
    const grandParentCellClone = grandParentCell ? { ...grandParentCell } : undefined
    delete grandParentCellClone?.controlList
    //
    const request: GridRequest2Dto = {
      list: [
        { grid: gridClone, cell: cellClone, control: control },
        { grid: parentGridClone, cell: parentCellClone, control: parentControl },
        { /* grid: grandParentGrid, */ cell: grandParentCellClone, control: grandParentControl }, // Request for GrandParent grid is not sent
      ]
    }
    const response = await pageGrid.serverApi.commandGridLoad2(request)
    if (response.list?.[0] != null) {
      // Load Patch
      const patchList = response.list[0].patchList
      if (patchList) {
        if (grid.rowCellList) {
          for (const patch of patchList) {
            for (const row of grid.rowCellList) {
              for (const cell of row) {
                if (cell.controlList) {
                  for (const control of cell.controlList) {
                    if (control.name == patch.controlName) {
                      control.isDisabled = patch.isDisabled
                      control.fileList = patch.fileList
                    }
                  }
                }
              }
            }
          }
        }
        grid.state = response.list[0].state
        pageGrid.grid.set({ ...grid })
      }
      // Load
      else {
        pageGrid.grid.set(response.list[0])
      }
    }
    if (response.list?.[1] != null) {
      pageGrid?.parent?.grid.set(response.list[1])
    }
    // Response never changes GrandParent
  }

  GridCellEnum = GridCellEnum

  GridControlEnum = GridControlEnum

  grid = signal<GridDto | undefined>(undefined)

  async load2(gridName: string | undefined, isLoad: boolean = true) {
    if (gridName) {
      const grid = { gridName: gridName }
      this.grid.set(grid)
      await this.gridSetWait(grid)
      if (isLoad && this.serverApi.isBrowser()) {
        await PageGrid.commandGridLoad2(this, undefined, undefined)
      }
    } else {
      this.grid.set(undefined)
    }
  }

  _grid?: GridDto // Data grid

  protected _lookup?: Lookup

  protected lookup = signal<Lookup | undefined>(undefined)

  @Input() protected parent?: PageGrid // Lookup parent

  @Input() detailList?: PageGrid[] // Detail grids to reload if new row is selected on this grid.

  cellTextGet(cell: GridCellDto) {
    switch (cell.cellEnum) {
      // Field
      case GridCellEnum.Field:
      case GridCellEnum.FieldCheckbox:
      case GridCellEnum.FieldAutocomplete:
      case GridCellEnum.FieldDropdown: {
        return cell.textModified ?? cell.text
      }
      // Button SelectMulti
      case GridCellEnum.CheckboxSelectMulti: {
        if (this._grid?.state?.isSelectMultiList && cell.dataRowIndex != undefined) {
          return this._grid?.state?.isSelectMultiList[cell.dataRowIndex] ? 'true' : 'false'
        } else {
          return 'false'
        }
      }
    }
    return undefined
  }

  cellTextGetControl(cell: GridCellDto, control: GridControlDto) {
    switch (control.controlEnum) {
      // Field
      case GridControlEnum.FieldCustom: {
        return control.text
      }
      // SelectMultiAll
      case GridControlEnum.CheckboxSelectMultiAll: {
        return this._grid?.state?.isSelectMultiAll ? 'true' : false
      }
    }
    return undefined
  }

  cellTextGetControlIndeterminate(cell: GridCellDto, control: GridControlDto) {
    switch (control.controlEnum) {
      // SelectMultiAll
      case GridControlEnum.CheckboxSelectMultiAll: {
        let result = this._grid?.state?.isSelectMultiIndeterminate
        if (this._grid?.state?.isSelectMultiList) {
          const values = Object.values(this._grid?.state?.isSelectMultiList)
          const isIndeterminate = !values.every((value, index, list) => index > 0 && value == list[index - 1] ? true : index == 0)
          result = isIndeterminate ? true : result
        }
        return result ? 'true' : 'false'
      }
    }
    return undefined
  }

  cellTextSetSave(cell: GridCellDto) {
    if (this._grid) {
      if (!this._grid.state) {
        this._grid.state = {}
      }
      if (!this._grid.state.fieldSaveList) {
        this._grid.state.fieldSaveList = []
      }
      if (cell.fieldName && cell.dataRowIndex != undefined) {
        let index = this._grid.state.fieldSaveList.findIndex(item => item.dataRowIndex == cell.dataRowIndex && item.fieldName == cell.fieldName)
        if (cell.textModified == undefined) {
          if (index != -1) {
            this._grid.state.fieldSaveList.splice(index, 1) // Remove item
          }
        } else {
          if (index == -1) {
            index = this._grid.state.fieldSaveList.push({}) - 1
          }
          this._grid.state.fieldSaveList[index] = {
            cellEnum: cell.cellEnum,
            dataRowIndex: cell.dataRowIndex,
            fieldName: cell.fieldName,
            text: cell.text,
            textModified: cell.textModified
          }
        }
      }
    }
  }

  cellTextSetTimeout?: NodeJS.Timeout
  async cellTextSetDebounce(cell: GridCellDto, value: string) {
    if (!this.cellTextSetTimeout) {
      await this.cellTextSet(cell, value) // Immediateley
      this.cellTextSetTimeout = setTimeout(async () => {
        this.cellTextSetTimeout = undefined
      }, 300)
    } else {
      clearTimeout(this.cellTextSetTimeout) // Remove pending one
      this.cellTextSetTimeout = setTimeout(async () => {
        await this.cellTextSet(cell, value)
        this.cellTextSetTimeout = undefined
      }, 300)
    }
  }

  async cellTextSet(cell: GridCellDto, value: string) {
    if (this._grid) {
      switch (cell.cellEnum) {
        // Field
        case GridCellEnum.Field: {
          cell.textModified = UtilClient.normalizeString(cell.text) != UtilClient.normalizeString(value) ? value : undefined
          this.cellTextSetSave(cell)
          break
        }
        // Autocomplete, Dropdown
        case GridCellEnum.FieldAutocomplete:
        case GridCellEnum.FieldDropdown: {
          cell.textModified = UtilClient.normalizeString(cell.text) != UtilClient.normalizeString(value) ? value : undefined
          this.cellTextSetSave(cell)
          break
        }
        // Filter
        case GridCellEnum.Filter: {
          if (!this._grid.state) {
            this._grid.state = {}
          }
          this._grid.state.filterList ??= {}
          this._grid.state.filterList[cell.fieldName!] = value
          if (value == '') {
            delete this._grid.state.filterList[cell.fieldName!] // Remove
          }
          await PageGrid.commandGridLoad2(this, cell, undefined)
          break
        }
        // CheckBox
        case GridCellEnum.FieldCheckbox: {
          const valueText = value ? 'true' : 'false'
          cell.textModified = cell.text != valueText ? valueText : undefined
          this.cellTextSetSave(cell)
          break
        }
        // SelectMulti
        case GridCellEnum.CheckboxSelectMulti: {
          if (this._grid) {
            if (cell.dataRowIndex != undefined) {
              if (!this._grid.state) {
                this._grid.state = {}
              }
              if (!this._grid.state.isSelectMultiList) {
                this._grid.state.isSelectMultiList = []
              }
              this._grid.state.isSelectMultiList[cell.dataRowIndex] = value == 'true' ? true : false
            }
          }
          if (this._grid.state?.isPatch) {
            await PageGrid.commandGridLoad2(this, cell, undefined)
          }
          break
        }
      }
    }
  }

  async cellTextSetControl(cell: GridCellDto, control: GridControlDto, value: string) {
    if (this._grid) {
      switch (control.controlEnum) {
        // Checkbox SelectMultiAll
        case GridControlEnum.CheckboxSelectMultiAll: {
          if (this._grid) {
            if (!this._grid.state) {
              this._grid.state = {}
            }
            this._grid.state.isSelectMultiAll = value == 'true'
            this._grid.state.isSelectMultiList ??= []
            if (this._grid.rowCellList) {
              for (const row of this._grid.rowCellList) {
                for (const cell of row) {
                  if (cell.cellEnum == GridCellEnum.CheckboxSelectMulti && cell.dataRowIndex != undefined) {
                    this._grid.state.isSelectMultiList[cell.dataRowIndex] = value == 'true'
                  }
                }
              }
            }
            if (this._grid.state?.isPatch) {
              await PageGrid.commandGridLoad2(this, cell, control)
            }
          }
          break
        }
        // Field Custom
        case GridControlEnum.FieldCustom: {
          control.textModified = UtilClient.normalizeString(control.text) != UtilClient.normalizeString(value) ? value : undefined
          this._grid.state ??= {}
          this._grid.state.fieldCustomSaveList ??= []
          let index = this._grid.state.fieldCustomSaveList.findIndex(item => item.cell?.dataRowIndex == cell.dataRowIndex && item.control?.name == control.name)
          if (control.textModified == undefined) {
            if (index != -1) {
              this._grid.state.fieldCustomSaveList.splice(index, 1) // Remove item
            }
          } else {
            if (index == -1) {
              index = this._grid.state.fieldCustomSaveList.push({}) - 1
            }
            this._grid.state.fieldCustomSaveList[index] = {
              cell: {
                cellEnum: cell.cellEnum,
                dataRowIndex: cell.dataRowIndex,
                fieldName: cell.fieldName,
                text: cell.text,
                textModified: cell.textModified
              },
              control: control
            }
          }
          break
        }
      }
    }
  }

  cellFocus(cell: GridCellDto) {
    this.clickLookup(cell)
  }

  cellMouseEnter(cell: GridCellDto) {
    if (this._grid) {
      if (cell.dataRowIndex != undefined) {
        if (!this._grid.state) {
          this._grid.state = {}
        }
        if (!this._grid.state.isMouseEnterList) {
          this._grid.state.isMouseEnterList = []
        }
        this._grid.state.isMouseEnterList[cell.dataRowIndex] = true
      }
    }
  }

  cellMouseLeave(cell: GridCellDto) {
    if (this._grid) {
      if (cell.dataRowIndex != undefined) {
        this._grid.state!.isMouseEnterList![cell.dataRowIndex] = null
      }
    }
  }

  cellIsMouseEnter(cell: GridCellDto) {
    return this._grid?.state?.isMouseEnterList && cell.dataRowIndex != undefined && this._grid.state.isMouseEnterList[cell.dataRowIndex] == true
  }

  cellIsSelect(cell: GridCellDto) {
    return this._grid?.state?.isSelectList && cell.dataRowIndex != undefined && this._grid.state.isSelectList[cell.dataRowIndex] == true
  }

  cellClick(cell: GridCellDto) {
    if (this._grid) {
      if (cell.dataRowIndex != undefined) {
        const dataRowIndex = cell.dataRowIndex
        if (!this._grid.state) {
          this._grid.state = {}
        }
        this._grid.state.isSelectList = []
        this._grid.state.isSelectList[cell.dataRowIndex] = true
        // Reload detail data grids
        this.detailList?.forEach(async item => {
          if (item._grid) {
            if (!item._grid.state) {
              item._grid.state = {}
            }
            if (!item._grid.state.rowKeyMasterList) {
              item._grid.state.rowKeyMasterList = {}
            }
            // Set rowKey on detail grid
            if (this._grid?.gridName && this._grid.state?.rowKeyList) {
              const rowKey = this._grid.state?.rowKeyList[dataRowIndex]
              item._grid.state.rowKeyMasterList[this._grid?.gridName] = rowKey
            }
            // Reload detail grid
            await PageGrid.commandGridLoad2(item)
          }
        })
      }
    }
  }

  /** Default click for GridCell */
  async click(cell: GridCellDto) {
    if (this._grid) {
      switch (cell.cellEnum) {
        // Header
        case GridCellEnum.Header: {
          if (!this._grid.state) {
            this._grid.state = {}
          }
          this._grid.state.sortList ??= []
          const sort = this._grid.state.sortList.length > 0 ? this._grid.state.sortList[0] : null // First or default
          if (sort && sort.fieldName == cell.fieldName) {
            sort.isDesc = !sort.isDesc
          } else {
            this._grid.state.sortList.unshift({ fieldName: cell.fieldName!, isDesc: false }) // Insert at 0
          }
          this._grid.state.pagination = this._grid.state.pagination ?? {}
          this._grid.state.pagination.pageIndex = 0
          await PageGrid.commandGridLoad2(this, cell, undefined)
          break
        }
      }
    }
  }

  async clickControl(cell: GridCellDto, control: GridControlDto) {
    if (this._grid) {
      switch (control.controlEnum) {
        // Button Reload
        case GridControlEnum.ButtonReload: {
          this.lookupClose()
          await PageGrid.commandGridLoad2(this, cell, control)
          break
        }
        // Button Save
        case GridControlEnum.ButtonSave: {
          this.lookupClose()
          await PageGrid.commandGridLoad2(this, cell, control)
          break
        }
        // Button Cancel (Lookup)
        case GridControlEnum.ButtonLookupCancel: {
          this.parent?.lookupClose()
          break
        }
        // Button Ok (Lookup)
        case GridControlEnum.ButtonLookupOk: {
          if (this.parent?._grid) {
            await PageGrid.commandGridLoad2(this, cell, control)
            this.parent?.lookupClose()
          }
          break
        }
        // Button Sort (Lookup)
        case GridControlEnum.ButtonLookupSort: {
          if (this.parent?._grid) {
            if (!this.parent._grid.state) {
              this.parent._grid.state = {}
            }
            this.parent.lookupClose()
          }
          break
        }
        // Button Column
        case GridControlEnum.ButtonColumn: {
          this.clickLookup(cell, control)
          break
        }
        // Button
        case GridControlEnum.Button: {
          this.lookupClose()
          if (!this._grid.state) {
            this._grid.state = {}
          }
          await PageGrid.commandGridLoad2(this, cell, control)
          break
        }
        // Button Custom
        case GridControlEnum.ButtonCustom: {
          this.lookupClose()
          if (!this._grid.state) {
            this._grid.state = {}
          }
          switch (control.fileEnum) {
            case GridFileEnum.Download: {
              const isPatch = control.isPatch
              control.isPatch = true
              await PageGrid.commandGridLoad2(this, cell, control)
              control.isPatch = isPatch
              const fileListResponse = control.fileList
              if (fileListResponse) {
                for (const file of fileListResponse) {
                  if (file?.fileName && file.fileUrl) {
                    var blob = await this.serverApi.fileDownload(file.fileName, file.fileUrl)
                    const url = URL.createObjectURL(blob)
                    const anchor = document.createElement('a')
                    anchor.href = url
                    anchor.download = file.fileName
                    anchor.style.display = 'none';
                    document.body.appendChild(anchor);
                    anchor.click();
                    document.body.removeChild(anchor);
                    URL.revokeObjectURL(url)
                  }
                }
              }
              break
            }
            case GridFileEnum.Upload: {
              const inputElement = document.createElement('input');
              inputElement.type = 'file';
              inputElement.multiple = true;
              inputElement.addEventListener('change', async (event: Event) => {
                const target = event.target as HTMLInputElement;
                const files = target.files;
                if (files && files.length > 0) {
                  const fileList = control.fileList
                  const isPatch = control.isPatch
                  const fileListRequest = Array.from(files, item => { return { fileName: item.name } })
                  control.fileList = fileListRequest
                  control.isPatch = true
                  await PageGrid.commandGridLoad2(this, cell, control)
                  const fileListResponse = control.fileList
                  for (const file of fileListResponse) {
                    if (file?.fileName && file.fileUrl) {
                      const fileSingle = Array.from(files).filter(item => item.name == file.fileName)
                      if (fileSingle.length == 1) {
                        await this.serverApi.fileUpload(fileSingle[0], file.fileUrl)
                      }
                    }
                  }
                  control.fileList = fileList
                  control.isPatch = isPatch
                  if (!isPatch) {
                    await PageGrid.commandGridLoad2(this, cell, control) // Reload
                  }
                }
                (event.target as HTMLInputElement).value = "" // Necessary to select and upload same file multiple times.
              })
              inputElement.click()
              break
            }
            default: {
              await PageGrid.commandGridLoad2(this, cell, control)
            }
          }
          break
        }
      }
    }
  }

  /** Open lookup window (or modal)*/
  async clickLookup(cell: GridCellDto, control?: GridControlDto) {
    if (this._grid) {
      if (this.lookup() == undefined) {
        this.lookup.set({} as Lookup) // Lookup open (not yet loaded grid)
      }
      const lookup = this.lookup()!
      lookup.cell = cell
      lookup.control = control
      lookup.grid ??= signal<GridDto>({ gridName: this._grid?.gridName })
      if (control?.controlEnum == GridControlEnum.ButtonModal || control?.controlEnum == GridControlEnum.ButtonModalCustom) {
        lookup.isModal = true
      }
      // Load lookup see ngAfterViewInit
    }
  }

  async clickPagination(cell: GridCellDto, control: GridControlDto, indexDelta: number) {
    if (this._grid) {
      this._grid.state = this._grid.state || {}
      this._grid.state.pagination = this._grid.state.pagination || {}
      this._grid.state.pagination.pageIndexDeltaClick = indexDelta
      await PageGrid.commandGridLoad2(this, cell, control)
    }
  }

  async clickBreadcrumb(cell: GridCellDto, control: GridControlDto, index: number) {
    const controlCopy = { ...control }
    controlCopy.text = index.toString()
    await PageGrid.commandGridLoad2(this, cell, controlCopy)
  }

  lookupClose() {
    this.lookup.set(undefined)
  }

  isFilterMulti(fieldName?: string) {
    if (fieldName) {
      return this._grid?.state?.filterMultiList && fieldName in this._grid.state.filterMultiList
    }
    return false
  }

  @ViewChild('tableRef') tableRef!: ElementRef;

  @HostListener('document:mouseup')
  onMouseUp() {
    if (this.resize) {
      this.resize = undefined
    }
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (this.resize) {
      const columnWidthDiff = event.clientX - (this.resize.cellClientX ?? 0)
      if (this._grid?.state?.columnWidthList) {
        const widhtDiff = 100 / this.resize.tableWidth * columnWidthDiff
        let widthSum = 0
        const columnList = [...this.resize.columnWidthList]
        for (let i = 0; i < columnList.length; i++) {
          const width = columnList[i]!
          if (i == columnList.length - 1) {
            // Column last
            columnList[i] = UtilClient.MathRound100(100 - widthSum) // 100.00 - 80.04 = 19.959999999999994 !
          } else {
            if (i < this.resize.columnIndex) {
              // Column left to resize
              columnList[i] = width
            } else {
              if (i == this.resize.columnIndex) {
                // Column resize
                columnList[i] = UtilClient.MathRound100(width + UtilClient.MathFloor100(widhtDiff))
              } else {
                // Column right to resize
                const widthRight = this.resize.columnWidthList.slice(this.resize.columnIndex + 1).reduce((sum, value) => sum! + value!, 0)!
                columnList[i] = UtilClient.MathRound100(width - UtilClient.MathFloor100(widhtDiff / widthRight * this.resize.columnWidthList[i]!))
              }
            }
          }
          widthSum = UtilClient.MathRound100(widthSum + columnList[i]!)
        }
        this._grid.state.columnWidthList = columnList
      }
    }
  }

  resize?: Resize

  /** Column resize */
  headerMouseDown(e: MouseEvent, cell: GridCellDto, cellIndex: number) {
    e.preventDefault()
    this.resize = {
      cell: cell,
      cellClientX: e.clientX,
      cellWidth: (e.target as HTMLElement).closest('td')!.offsetWidth,
      columnIndex: cellIndex,
      columnCount: 0,
      tableWidth: this.tableRef.nativeElement.clientWidth,
      columnWidthList: []
    }
    const table = this.tableRef.nativeElement
    // ColumnCount
    for (const row of table.rows) {
      this.resize.columnCount = Math.max(this.resize.columnCount, row.cells.length)
    }
    this.resize.tableWidth = this.tableRef.nativeElement.clientWidth
    if (this._grid) {
      if (!this._grid.state) {
        this._grid.state = {}
      }
      if (!this._grid.state.columnWidthList) {
        this._grid.state.columnWidthList = []
      }
      if (this._grid.state.columnWidthList.length < this.resize.columnCount) {
        const widthAvg = UtilClient.MathFloor100(100 / this.resize.columnCount)
        let widthSum = 0
        const columnWidthList = this._grid.state.columnWidthList
        for (let i = 0; i < this.resize.columnCount; i++) {
          if (i < this.resize.columnCount - 1) {
            columnWidthList[i] = widthAvg
            widthSum += widthAvg
          } else {
            columnWidthList[i] = 100 - widthSum
          }
        }
      }
    }
    this.resize.columnWidthList = this._grid!.state!.columnWidthList!
  }
}

class Lookup {
  cell?: GridCellDto // Cell with open lookup

  control?: GridControlDto // Control with open lookup

  grid!: WritableSignal<GridDto> // Lookup window

  isModal?: boolean // Lookup window is modal
}

class Resize {
  cell!: GridCellDto
  cellClientX!: number
  cellWidth!: number
  columnIndex!: number
  columnCount!: number
  tableWidth!: number
  columnWidthList!: (number | null)[]
}