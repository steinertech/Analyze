import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, Input, ViewChild } from '@angular/core';
import { GridControlDto, GridControlEnum, GridCellDto, GridCellEnum, GridDto, ServerApi } from '../generate';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-page-grid',
  imports: [
    CommonModule, // Json pipe
    FormsModule, // Used for ngModels
  ],
  templateUrl: './page-grid.component.html',
  styleUrl: './page-grid.component.css'
})
export class PageGridComponent {
  constructor(private serverApi: ServerApi) {

  }

  GridCellEnum = GridCellEnum

  GridControlEnum = GridControlEnum

  @Input() grid?: GridDto // Data grid

  lookup?: Lookup

  @Input() protected parent?: PageGridComponent // Lookup parent

  @Input() detailList?: PageGridComponent[] // Detail grids to reload if new row is selected on this grid.

  cellTextGet(cell: GridCellDto) {
    switch (cell.cellEnum) {
      // Field
      case GridCellEnum.Field: {
        return cell.textModified ?? cell.text
      }
      // Filter
      case GridCellEnum.Filter: {
        return this.grid?.state?.filterList?.find(value => value.fieldName == cell.fieldName)?.text
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
        if (this.grid?.state?.isSelectMultiList && cell.dataRowIndex != undefined) {
          return this.grid?.state?.isSelectMultiList[cell.dataRowIndex] ? 'true' : 'false'
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
    if (this.grid) {
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
          if (!this.grid.state) {
            this.grid.state = {}
          }
          if (!this.grid.state.filterList) {
            this.grid.state.filterList = []
          }
          let index = this.grid.state.filterList.findIndex(value => value.fieldName == cell.fieldName)
          if (index == -1) {
            index = this.grid.state.filterList.push({ fieldName: cell.fieldName!, text: undefined! }) - 1 // Add
          }
          if (value == '') {
            this.grid.state.filterList.splice(index, 1) // Remove
          } else {
            this.grid.state.filterList[index].text = value
          }
          this.serverApi.commandGridLoad(this.grid, this.parent?.lookup?.cell, this.parent?.lookup?.control, this.parent?.grid).subscribe(value => this.grid = value); // Reload // TODO Debounce
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
          if (this.grid) {
            if (cell.dataRowIndex != undefined) {
              if (!this.grid.state) {
                this.grid.state = {}
              }
              if (!this.grid.state.isSelectMultiList) {
                this.grid.state.isSelectMultiList = []
              }
              this.grid.state.isSelectMultiList[cell.dataRowIndex] = value == 'true' ? true : false
            }
          }
          break
        }
      }
    }
  }

  cellTextSetControl(cell: GridCellDto, control: GridControlDto, value: string) {
    if (this.grid) {
      switch (control.controlEnum) {
        // Checkbox SelectMultiAll
        case GridControlEnum.CheckboxSelectMultiAll: {
          if (this.grid) {
            if (!this.grid.state) {
              this.grid.state = {}
            }
            if (!this.grid.state.isSelectMultiList) {
              this.grid.state.isSelectMultiList = []
            }
            if (this.grid.rowCellList) {
              for (let rowIndex = 0; rowIndex < this.grid.rowCellList.length; rowIndex++) {
                let row = this.grid.rowCellList[rowIndex]
                for (let cellIndex = 0; cellIndex < row.length; cellIndex++) {
                  let cell = row[cellIndex]
                  if (cell.cellEnum == GridCellEnum.CheckboxSelectMulti && cell.dataRowIndex != undefined) {
                    this.grid.state.isSelectMultiList[cell.dataRowIndex] = value == 'true'
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
    if (this.grid) {
      if (cell.dataRowIndex != undefined) {
        if (!this.grid.state) {
          this.grid.state = {}
        }
        if (!this.grid.state.isMouseEnterList) {
          this.grid.state.isMouseEnterList = []
        }
        this.grid.state.isMouseEnterList[cell.dataRowIndex] = true
      }
    }
  }

  cellMouseLeave(cell: GridCellDto) {
    if (this.grid) {
      if (cell.dataRowIndex != undefined) {
        this.grid.state!.isMouseEnterList![cell.dataRowIndex] = null
      }
    }
  }

  cellIsMouseEnter(cell: GridCellDto) {
    return this.grid?.state?.isMouseEnterList && cell.dataRowIndex != undefined && this.grid.state.isMouseEnterList[cell.dataRowIndex] == true
  }

  cellIsSelect(cell: GridCellDto) {
    return this.grid?.state?.isSelectList && cell.dataRowIndex != undefined && this.grid.state.isSelectList[cell.dataRowIndex] == true
  }

  cellClick(cell: GridCellDto) {
    if (this.grid) {
      if (cell.dataRowIndex != undefined) {
        let dataRowIndex = cell.dataRowIndex
        if (!this.grid.state) {
          this.grid.state = {}
        }
        this.grid.state.isSelectList = []
        this.grid.state.isSelectList[cell.dataRowIndex] = true
        // Reload detail data grids
        this.detailList?.forEach(item => {
          if (item.grid) {
            if (!item.grid.state) {
              item.grid.state = {}
            }
            if (!item.grid.state.rowKeyMasterList) {
              item.grid.state.rowKeyMasterList = {}
            }
            // Set rowKey on detail grid
            if (this.grid?.gridName && this.grid.state?.rowKeyList) {
              let rowKey = this.grid.state?.rowKeyList[dataRowIndex]
              item.grid.state.rowKeyMasterList[this.grid?.gridName] = rowKey
            }
            // Reload detail grid
            this.serverApi.commandGridLoad(item.grid, item.parent?.lookup?.cell, item.parent?.lookup?.control, item.parent?.grid).subscribe(value => item.grid = value);
          }
        })
      }
    }
  }

  /** Default click for GridCell */
  click(cell: GridCellDto) {
    if (this.grid) {
      switch (cell.cellEnum) {
        // Header
        case GridCellEnum.Header: {
          if (!this.grid.state) {
            this.grid.state = {}
          }
          if (!this.grid.state.sort) {
            this.grid.state.sort = { fieldName: undefined!, isDesc: false }
          }
          if (this.grid.state.sort.fieldName == cell.fieldName) {
            this.grid.state.sort.isDesc = !this.grid.state.sort.isDesc
          } else {
            this.grid.state.sort = { fieldName: cell.fieldName!, isDesc: false }
          }
          this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value); // Reload
          break
        }
      }
    }
  }

  clickControl(cell: GridCellDto, control: GridControlDto) {
    if (this.grid) {
      switch (control.controlEnum) {
        // Button Reload
        case GridControlEnum.ButtonReload: {
          this.grid.state = undefined // Clear state
          this.lookupClose()
          this.serverApi.commandGridLoad(this.grid, this.parent?.lookup?.cell, this.parent?.lookup?.control, this.parent?.grid).subscribe(value => {
            this.grid = value // Reload
          });
          break
        }
        // Button Save
        case GridControlEnum.ButtonSave: {
          this.lookupClose()
          this.serverApi.commandGridSave(this.grid, this.parent?.lookup?.cell, this.parent?.lookup?.control, this.parent?.grid).subscribe(value => {
            this.grid = value.grid
            if (this.parent?.grid && value.parentGrid) {
              this.parent.grid = value.parentGrid
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
          if (this.parent?.grid) {
            this.serverApi.commandGridSave(this.grid, this.parent.lookup?.cell, this.parent.lookup?.control, this.parent.grid).subscribe(value => {
              this.grid = value.grid // Lookup to be closed
              if (this.parent?.grid && value.parentGrid) {
                this.parent.grid = value.parentGrid // Parent reload
              }
              this.parent?.lookupClose()
            });
          }
          break
        }
        // Button Sort (Lookup)
        case GridControlEnum.ButtonLookupSort: {
          if (this.parent?.grid) {
            if (!this.parent.grid.state) {
              this.parent.grid.state = {}
            }
            if (!this.parent.grid.state.sort) {
              this.parent.grid.state.sort = { fieldName: this.parent.lookup?.cell?.fieldName!, isDesc: false }
            }
            if (this.parent.grid.state.sort.fieldName != this.parent.lookup?.cell?.fieldName) {
              this.parent.grid.state.sort = { fieldName: this.parent.lookup?.cell?.fieldName!, isDesc: false }
            }
            else {
              this.parent.grid.state.sort.isDesc = !this.parent.grid.state.sort.isDesc
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
          if (!this.grid.state) {
            this.grid.state = {}
          }
          this.grid.state.buttonCustomClick = { name: control.name, dataRowIndex: cell.dataRowIndex, fieldName: cell.fieldName }
          this.serverApi.commandGridSave(this.grid, this.parent?.lookup?.cell, this.parent?.lookup?.control, this.parent?.grid).subscribe(value => this.grid = value.grid);
          break
        }
      }
    }
  }

  clickLookup(cell: GridCellDto, control?: GridControlDto) {
    if (this.grid) {
      this.lookup = { cell: cell, control: control } // Lookup open
      if (control?.controlEnum == GridControlEnum.ButtonModal) {
        this.lookup.isModal = true
      }
      let lookup = this.lookup
      lookup.grid = { gridName: this.grid?.gridName }
      this.serverApi.commandGridLoad(lookup.grid, lookup.cell, lookup.control, this.grid).subscribe(value => lookup.grid = value);
    }
  }

  clickPagination(pageIndexClick?: number) {
    if (this.grid) {
      if (!this.grid.state) {
        this.grid.state = {}
      }
      if (!this.grid.state.pagination) {
        this.grid.state.pagination = {}
      }
      this.grid.state.pagination.pageIndexClick = pageIndexClick
      this.serverApi.commandGridLoad(this.grid, this.parent?.lookup?.cell, this.parent?.lookup?.control, this.parent?.grid).subscribe(value => { this.grid = value });
    }
  }

  lookupClose() {
    this.lookup = undefined
  }

  isFilterMulti(fieldName?: string) {
    if (fieldName) {
      return (this.grid?.state?.filterMultiList?.map(item => item.fieldName).indexOf(fieldName) ?? - 1) >= 0
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
      let columnWidthDiff = event.clientX - (this.resize?.cellClientX ?? 0)
      if (this.grid?.state?.columnWidthList) {
        this.grid.state.columnWidthList[this.resize.columnIndex ?? -1] = this.resize.cellWidth! + columnWidthDiff
      }
    }
  }

  resize?: Resize

  headerMouseDown(e: MouseEvent, cell: GridCellDto, cellIndex: number) {
    e.preventDefault()
    this.resize = {}
    this.resize.cell = cell
    this.resize.cellClientX = e.clientX
    this.resize.cellWidth = (e.target as HTMLElement).parentElement?.offsetWidth
    this.resize.columnIndex = cellIndex
    let table = this.tableRef.nativeElement
    // ColumnCount
    this.resize.columnCount = 0
    for (let rowIndex = 0; rowIndex < table.rows.length; rowIndex++) {
      this.resize.columnCount = Math.max(this.resize.columnCount, table.rows[rowIndex].cells.length)
    }
    this.resize.tableWidth = this.tableRef.nativeElement.clientWidth
    if (this.grid) {
      if (!this.grid.state) {
        this.grid.state = {}
      }
      if (!this.grid.state.columnWidthList) {
        this.grid.state.columnWidthList = []
      }
      if (this.grid.state.columnWidthList.length < this.resize.columnCount) {
        this.grid.state.columnWidthList[this.resize.columnCount - 1] = null
      }
    }
  }
}

class Lookup {
  cell?: GridCellDto // Cell with open lookup

  control?: GridControlDto // Control with open lookup

  grid?: GridDto // Lookup window

  isModal?: boolean // Lookup window is modal
}

class Resize {
  cell?: GridCellDto
  cellClientX?: number
  cellWidth?: number
  columnIndex?: number
  columnCount?: number
  tableWidth?: number
}
