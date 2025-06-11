import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { ControlDto, ControlEnum, GridCellDto, GridCellEnum, GridDto, ServerApi } from '../generate';
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

  ControlEnum = ControlEnum

  @Input() grid?: GridDto // Data grid

  lookupCell?: GridCellDto // Cell with open lookup

  @Input() lookupGrid?: GridDto // Lookup window

  @Input() parent?: PageGridComponent // Lookup parent

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

  cellTextGetControl(cell: GridCellDto, control: ControlDto) {
    switch (control.controlEnum) {
      // Field
      case ControlEnum.CheckboxSelectMultiAll: {
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
          this.serverApi.commandGridLoad(this.grid, this.parent?.lookupCell, this.parent?.grid).subscribe(value => this.grid = value); // Reload // TODO Debounce
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

  cellTextSetControl(cell: GridCellDto, control: ControlDto, value: string) {
    if (this.grid) {
      switch (control.controlEnum) {
        // SelectMultiAll
        case ControlEnum.CheckboxSelectMultiAll: {
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
        if (!this.grid.state) {
          this.grid.state = {}
        }
        this.grid.state.isSelectList = []
        this.grid.state.isSelectList[cell.dataRowIndex] = true
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

  clickControl(cell: GridCellDto, control: ControlDto) {
    if (this.grid) {
      switch (control.controlEnum) {
        // Button Cancel
        case ControlEnum.ButtonCancel: {
          this.grid.state = undefined // Clear state
          this.lookupClose()
          this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value); // Reload
          break
        }
        // Button Save
        case ControlEnum.ButtonSave: {
          this.lookupClose()
          this.serverApi.commandGridSave(this.grid).subscribe(value => this.grid = value);
          break
        }
        // Button Cancel (Lookup)
        case ControlEnum.ButtonLookupCancel: {
          this.parent?.lookupClose()
          break
        }
        // Button Ok (Lookup)
        case ControlEnum.ButtonLookupOk: {
          if (this.parent?.grid) {
            this.serverApi.commandGridSave(this.grid, this.parent.lookupCell, this.parent.grid).subscribe(value => {
              if (this.parent) {
                this.parent.grid = value
                this.parent.lookupClose()
              }
            });
          }
          break
        }
        // Button Sort (Lookup)
        case ControlEnum.ButtonLookupSort: {
          if (this.parent?.grid) {
            if (!this.parent.grid.state) {
              this.parent.grid.state = {}
            }
            if (!this.parent.grid.state.sort) {
              this.parent.grid.state.sort = { fieldName: this.parent.lookupCell?.fieldName!, isDesc: false }
            }
            if (this.parent.grid.state.sort.fieldName != this.parent.lookupCell?.fieldName) {
              this.parent.grid.state.sort = { fieldName: this.parent.lookupCell?.fieldName!, isDesc: false }
            }
            else {
              this.parent.grid.state.sort.isDesc = !this.parent.grid.state.sort.isDesc
            }
            this.parent.lookupClose()
          }
          break
        }
        // Button Column
        case ControlEnum.ButtonColumn: {
          if (this.grid) {
            if (this.lookupCell == cell) {
              this.lookupCell = undefined // Lookup close
            } else {
              this.lookupCell = cell // Lookup open
            }
            if (this.lookupCell) {
              this.lookupGrid = { gridName: this.grid?.gridName }
              this.serverApi.commandGridLoad(this.lookupGrid, this.lookupCell, this.grid).subscribe(value => this.lookupGrid = value);
            }
          }
          break
        }
        // Button Custom
        case ControlEnum.ButtonCustom: {
          this.lookupClose()
          control.isClick = true
          this.serverApi.commandGridSave(this.grid).subscribe(value => this.grid = value);
          break
        }
      }
    }
  }

  clickLookup(cell: GridCellDto) {
    if (this.grid) {
      if (this.lookupCell == cell) {
        this.lookupCell = undefined // Lookup close
      } else {
        this.lookupCell = cell // Lookup open
      }
      if (this.lookupCell) {
        this.lookupGrid = { gridName: this.grid?.gridName }
        this.serverApi.commandGridLoad(this.lookupGrid, this.lookupCell, this.grid).subscribe(value => this.lookupGrid = value);
      }
    }
  }

  lookupClose() {
    this.lookupCell = undefined
    this.lookupGrid = undefined
  }

  isFilterMulti(fieldName?: string) {
    if (fieldName) {
      return (this.grid?.state?.filterMultiList?.map(item => item.fieldName).indexOf(fieldName) ?? - 1) >= 0
    }
    return false
  }
}
