import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, ElementRef, HostListener, inject, Input, signal, ViewChild, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { GridCellDto, GridCellEnum, GridControlDto, GridControlEnum, GridDto, ServerApi } from '../generate';
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
export class PageGrid {
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

  GridCellEnum = GridCellEnum

  GridControlEnum = GridControlEnum

  @Input() grid!: WritableSignal<GridDto>

  _grid?: GridDto // Data grid

  protected _lookup?: Lookup

  protected lookup = signal<Lookup | undefined>(undefined)

  @Input() protected parent?: PageGrid // Lookup parent

  @Input() detailList?: PageGrid[] // Detail grids to reload if new row is selected on this grid.

  cellTextGet(cell: GridCellDto) {
    switch (cell.cellEnum) {
      // Field
      case GridCellEnum.Field: {
        return cell.textModified ?? cell.text
      }
      // Field Checkbox
      case GridCellEnum.FieldCheckbox: {
        return cell.textModified ?? cell.text
      }
      // Field Autocomplete
      case GridCellEnum.FieldAutocomplete: {
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
        const index = this._grid.state.fieldSaveList.findIndex(item => item.fieldName == cell.fieldName && item.dataRowIndex == cell.dataRowIndex)
        if (index != -1) {
          this._grid.state.fieldSaveList.splice(index) // Remove item
        }
        if (cell.textModified != undefined) {
          this._grid.state.fieldSaveList.push({
            fieldName: cell.fieldName,
            dataRowIndex: cell.dataRowIndex,
            text: cell.text,
            textModified: cell.textModified
          })
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
          cell.textModified = cell.text != value ? value : undefined
          this.cellTextSetSave(cell)
          break
        }
        // Field
        case GridCellEnum.FieldAutocomplete: {
          cell.textModified = cell.text != value ? value : undefined
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
          const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: undefined, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
          this.grid.set(response.grid)
          if (this.parent?._grid && response.parentGrid) {
            this.parent.grid.set(response.parentGrid)
          }
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
          break
        }
      }
    }
  }

  cellTextSetControl(cell: GridCellDto, control: GridControlDto, value: string) {
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
          }
          break
        }
        // Field Custom
        case GridControlEnum.FieldCustom: {
          control.textModified = control.text != value ? value : undefined
          this.cellTextSetSave(cell)
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
            const response = await this.serverApi.commandGridLoad({ grid: item._grid, cell: cell, parentCell: item.parent?._lookup?.cell, parentControl: item.parent?._lookup?.control, parentGrid: item.parent?._grid })
            item.grid.set(response.grid);
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
          if (!this._grid.state.sort) {
            this._grid.state.sort = { fieldName: undefined!, isDesc: false }
          }
          if (this._grid.state.sort.fieldName == cell.fieldName) {
            this._grid.state.sort.isDesc = !this._grid.state.sort.isDesc
          } else {
            this._grid.state.sort = { fieldName: cell.fieldName!, isDesc: false }
          }
          this._grid.state.pagination = this._grid.state.pagination ?? {}
          this._grid.state.pagination.pageIndex = 0
          const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
          this.grid.set(response.grid); // Reload
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
          this._grid.state = undefined // Clear state
          this.lookupClose()
          const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: control, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
          this.grid.set(response.grid) // Reload
          break
        }
        // Button Save
        case GridControlEnum.ButtonSave: {
          this.lookupClose()
          const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: control, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
          this.grid.set(response.grid)
          if (this.parent?._grid && response.parentGrid) {
            this.parent.grid.set(response.parentGrid)
          }
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
            const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: control, parentCell: this.parent._lookup?.cell, parentControl: this.parent._lookup?.control, parentGrid: this.parent._grid })
            this.grid.set(response.grid) // Lookup to be closed
            if (this.parent?._grid && response.parentGrid) {
              this.parent.grid.set(response.parentGrid) // Parent reload
            }
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
            if (!this.parent._grid.state.sort) {
              // eslint-disable-next-line @typescript-eslint/no-non-null-asserted-optional-chain
              this.parent._grid.state.sort = { fieldName: this.parent._lookup?.cell?.fieldName!, isDesc: false }
            }
            if (this.parent._grid.state.sort.fieldName != this.parent._lookup?.cell?.fieldName) {
              // eslint-disable-next-line @typescript-eslint/no-non-null-asserted-optional-chain
              this.parent._grid.state.sort = { fieldName: this.parent._lookup?.cell?.fieldName!, isDesc: false }
            }
            else {
              this.parent._grid.state.sort.isDesc = !this.parent._grid.state.sort.isDesc
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
        // Button Custom
        case GridControlEnum.ButtonCustom: {
          this.lookupClose()
          if (!this._grid.state) {
            this._grid.state = {}
          }
          const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: control, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
          this.grid.set(response.grid);
          break
        }
      }
    }
  }

  /** Open lookup window */
  async clickLookup(cell: GridCellDto, control?: GridControlDto) {
    if (this._grid) {
      if (this.lookup() == undefined) {
        this.lookup.set({} as Lookup) // Lookup open (not yet loaded grid)
      }
      const lookup = this.lookup()!
      lookup.cell = cell
      lookup.control = control
      lookup.grid ??= signal<GridDto>({ gridName: this._grid?.gridName })
      if (control?.controlEnum == GridControlEnum.ButtonModal) {
        lookup.isModal = true
      }
      const response = await this.serverApi.commandGridLoad({ grid: lookup.grid(), cell: cell, control: control, parentCell: lookup.cell, parentControl: lookup.control, parentGrid: this._grid })
      lookup.grid.set(response.grid) // Lookup open (with loaded grid)
    }
  }

  async clickPagination(cell: GridCellDto, control: GridControlDto, indexDelta: number) {
    if (this._grid) {
      this._grid.state = this._grid.state || {}
      this._grid.state.pagination = this._grid.state.pagination || {}
      this._grid.state.pagination.pageIndexDeltaClick = indexDelta
      const response = await this.serverApi.commandGridLoad({ grid: this._grid, cell: cell, control: control, parentCell: this.parent?._lookup?.cell, parentControl: this.parent?._lookup?.control, parentGrid: this.parent?._grid })
      this.grid.set(response.grid);
      if (this.parent?._grid && response.parentGrid) {
        this.parent.grid.set(response.parentGrid)
      }
    }
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