import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, effect, ElementRef, HostListener, Input, model, signal, ViewChild } from '@angular/core';
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
  constructor(private serverApi: ServerApi) {
    effect(() => this._grid = this.grid())
  }

  GridCellEnum = GridCellEnum

  GridControlEnum = GridControlEnum

  readonly grid = model<GridDto>()

  _grid?: GridDto // Data grid

  readonly lookup = signal<Lookup | undefined>(undefined)

  @Input() protected parent?: PageGrid // Lookup parent

  @Input() detailList?: PageGrid[] // Detail grids to reload if new row is selected on this grid.

  cellTextGet(cell: GridCellDto) {
    switch (cell.cellEnum) {
      // Field
      case GridCellEnum.Field: {
        return cell.textModified ?? cell.text
      }
      // Filter
      case GridCellEnum.Filter: {
        return this._grid?.state?.filterList?.find(value => value.fieldName == cell.fieldName)?.text
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
    }
    return undefined
  }

  cellTextSet(cell: GridCellDto, value: string) {
    if (this._grid) {
      switch (cell.cellEnum) {
        // Field
        case GridCellEnum.Field: {
          cell.textModified = cell.text != value ? value : undefined
          break
        }
        // Field
        case GridCellEnum.FieldAutocomplete: {
          cell.textModified = cell.text != value ? value : undefined
          break
        }
        // Filter
        case GridCellEnum.Filter: {
          if (!this._grid.state) {
            this._grid.state = {}
          }
          if (!this._grid.state.filterList) {
            this._grid.state.filterList = []
          }
          let index = this._grid.state.filterList.findIndex(value => value.fieldName == cell.fieldName)
          if (index == -1) {
            index = this._grid.state.filterList.push({ fieldName: cell.fieldName!, text: undefined! }) - 1 // Add
          }
          if (value == '') {
            this._grid.state.filterList.splice(index, 1) // Remove
          } else {
            this._grid.state.filterList[index].text = value
          }
          this.serverApi.commandGridLoad(this._grid, this.parent?.lookup()?.cell, this.parent?.lookup()?.control, this.parent?._grid).subscribe(value => this.grid.set(value)); // Reload // TODO Debounce
          break
        }
        // CheckBox
        case GridCellEnum.FieldCheckbox: {
          let valueText = value ? 'true' : 'false'
          cell.textModified = cell.text != valueText ? valueText : undefined
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
            if (!this._grid.state.isSelectMultiList) {
              this._grid.state.isSelectMultiList = []
            }
            if (this._grid.rowCellList) {
              for (let rowIndex = 0; rowIndex < this._grid.rowCellList.length; rowIndex++) {
                let row = this._grid.rowCellList[rowIndex]
                for (let cellIndex = 0; cellIndex < row.length; cellIndex++) {
                  let cell = row[cellIndex]
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
        let dataRowIndex = cell.dataRowIndex
        if (!this._grid.state) {
          this._grid.state = {}
        }
        this._grid.state.isSelectList = []
        this._grid.state.isSelectList[cell.dataRowIndex] = true
        // Reload detail data grids
        this.detailList?.forEach(item => {
          if (item._grid) {
            if (!item._grid.state) {
              item._grid.state = {}
            }
            if (!item._grid.state.rowKeyMasterList) {
              item._grid.state.rowKeyMasterList = {}
            }
            // Set rowKey on detail grid
            if (this._grid?.gridName && this._grid.state?.rowKeyList) {
              let rowKey = this._grid.state?.rowKeyList[dataRowIndex]
              item._grid.state.rowKeyMasterList[this._grid?.gridName] = rowKey
            }
            // Reload detail grid
            this.serverApi.commandGridLoad(item._grid, item.parent?.lookup()?.cell, item.parent?.lookup()?.control, item.parent?._grid).subscribe(value => item.grid.set(value));
          }
        })
      }
    }
  }

  /** Default click for GridCell */
  click(cell: GridCellDto) {
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
          this.serverApi.commandGridLoad(this._grid).subscribe(value => this.grid.set(value)); // Reload
          break
        }
      }
    }
  }

  clickControl(cell: GridCellDto, control: GridControlDto) {
    if (this._grid) {
      switch (control.controlEnum) {
        // Button Reload
        case GridControlEnum.ButtonReload: {
          this._grid.state = undefined // Clear state
          this.lookupClose()
          this.serverApi.commandGridLoad(this._grid, this.parent?.lookup()?.cell, this.parent?.lookup()?.control, this.parent?._grid).subscribe(value => {
            this.grid.set(value) // Reload
          });
          break
        }
        // Button Save
        case GridControlEnum.ButtonSave: {
          this.lookupClose()
          this.serverApi.commandGridSave(this._grid, this.parent?.lookup()?.cell, this.parent?.lookup()?.control, this.parent?._grid).subscribe(value => {
            this.grid.set(value.grid)
            if (this.parent?._grid && value.parentGrid) {
              this.parent.grid.set(value.parentGrid)
            }
          });
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
            this.serverApi.commandGridSave(this._grid, this.parent.lookup()?.cell, this.parent.lookup()?.control, this.parent._grid).subscribe(value => {
              this.grid.set(value.grid) // Lookup to be closed
              if (this.parent?._grid && value.parentGrid) {
                this.parent.grid.set(value.parentGrid) // Parent reload
              }
              this.parent?.lookupClose()
            });
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
              this.parent._grid.state.sort = { fieldName: this.parent.lookup()?.cell?.fieldName!, isDesc: false }
            }
            if (this.parent._grid.state.sort.fieldName != this.parent.lookup()?.cell?.fieldName) {
              this.parent._grid.state.sort = { fieldName: this.parent.lookup()?.cell?.fieldName!, isDesc: false }
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
          this._grid.state.buttonCustomClick = { name: control.name, dataRowIndex: cell.dataRowIndex, fieldName: cell.fieldName }
          this.serverApi.commandGridSave(this._grid, this.parent?.lookup()?.cell, this.parent?.lookup()?.control, this.parent?._grid).subscribe(value => this.grid.set(value.grid));
          break
        }
      }
    }
  }

  clickLookup(cell: GridCellDto, control?: GridControlDto) {
    if (this._grid) {
      let lookup = <Lookup>{ cell: cell, control: control }

      if (control?.controlEnum == GridControlEnum.ButtonModal) {
        lookup.isModal = true
      }
      lookup.grid = { gridName: this._grid?.gridName }
      this.lookup.set(lookup) // Lookup open (not yet loaded grid)
      this.serverApi.commandGridLoad(lookup.grid, lookup.cell, lookup.control, this._grid).subscribe(value => {
        lookup.grid = value
        this.lookup.set({ ...lookup }) // Lookup open (with loaded grid)
      });
    }
  }

  clickPagination(pageIndexClick?: number) {
    if (this._grid) {
      if (!this._grid.state) {
        this._grid.state = {}
      }
      if (!this._grid.state.pagination) {
        this._grid.state.pagination = {}
      }
      this._grid.state.pagination.pageIndexClick = pageIndexClick
      this.serverApi.commandGridLoad(this._grid, this.parent?.lookup()?.cell, this.parent?.lookup()?.control, this.parent?._grid).subscribe(value => { this.grid.set(value) });
    }
  }

  lookupClose() {
    this.lookup.set(undefined)
  }

  isFilterMulti(fieldName?: string) {
    if (fieldName) {
      return (this._grid?.state?.filterMultiList?.map(item => item.fieldName).indexOf(fieldName) ?? - 1) >= 0
    }
    return false
  }

  @ViewChild('tableRef') tableRef!: ElementRef;

  @HostListener('document:mouseup', ['$event'])
  onMouseUp(event: MouseEvent) {
    if (this.resize) {
      this.resize = undefined
    }
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (this.resize) {
      let columnWidthDiff = event.clientX - (this.resize.cellClientX ?? 0)
      if (this._grid?.state?.columnWidthList) {
        let widhtDiff = 100 / this.resize.tableWidth * columnWidthDiff
        let widthSum = 0
        let columnList = [...this.resize.columnWidthList]
        for (let i = 0; i < columnList.length; i++) {
          let width = columnList[i]!
          if (i == columnList.length - 1) {
            // Column last
            columnList[i] = UtilClient.MathFloor100(100 - widthSum) // 100.00 - 80.04 = 19.959999999999994 !
          } else {
            if (i < this.resize.columnIndex) {
              // Column left to resize
              columnList[i] = width
            } else {
              if (i == this.resize.columnIndex) {
                // Column resize
                columnList[i] = UtilClient.MathFloor100(width + UtilClient.MathFloor100(widhtDiff))
              } else {
                // Column right to resize
                let widthRight = this.resize.columnWidthList.slice(this.resize.columnIndex + 1).reduce((sum, value) => sum! + value!, 0)!
                columnList[i] = UtilClient.MathFloor100(width - UtilClient.MathFloor100(widhtDiff / widthRight * this.resize.columnWidthList[i]!))
              }
            }
          }
          widthSum = UtilClient.MathFloor100(widthSum + columnList[i]!)
        }
        this._grid.state.columnWidthList = columnList
      }
    }
  }

  resize?: Resize

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
    let table = this.tableRef.nativeElement
    // ColumnCount
    for (let rowIndex = 0; rowIndex < table.rows.length; rowIndex++) {
      this.resize.columnCount = Math.max(this.resize.columnCount, table.rows[rowIndex].cells.length)
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
        let widthAvg = UtilClient.MathFloor100(100 / this.resize.columnCount)
        let widthSum = 0
        let columnWidthList = this._grid.state.columnWidthList
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

  grid?: GridDto // Lookup window

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