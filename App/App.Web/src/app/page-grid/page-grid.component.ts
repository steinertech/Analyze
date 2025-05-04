import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { GridCellDto, GridCellEnum, GridDto, ServerApi } from '../generate';
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

  @Input() grid?: GridDto

  lookupCell?: GridCellDto // Cell with open lookup

  @Input() lookupGrid?: GridDto

  @Input() parent?: PageGridComponent

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
      // Checkbox
      case GridCellEnum.FieldCheckbox: {
        return cell.textModified ?? cell.text
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
          this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value); // Reload // TODO Debounce
          break
        }
        // CheckBox
        case GridCellEnum.FieldCheckbox: {
          let valueText = value ? 'true' : 'false'
          cell.textModified = cell.text != valueText ? valueText : undefined
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

  cellIsEnter(cell: GridCellDto) {
    return this.grid?.state?.isMouseEnterList && cell.dataRowIndex != undefined && this.grid.state.isMouseEnterList[cell.dataRowIndex] == true
  }

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
        // Button Cancel
        case GridCellEnum.ButtonCancel: {
          this.grid.state = undefined // Clear state
          this.lookupClose()
          this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value); // Reload
          break
        }
        // Button Save
        case GridCellEnum.ButtonSave: {
          this.lookupClose()
          this.serverApi.commandGridSave(this.grid).subscribe(value => this.grid = value);
          break
        }
        // Button Cancel (Lookup)
        case GridCellEnum.ButtonLookupCancel: {
          this.parent?.lookupClose()
          break
        }
        // Button Ok (Lookup)
        case GridCellEnum.ButtonLookupOk: {
          if (this.parent?.grid) {
            this.serverApi.commandGridSave(this.grid).subscribe(value => {
              if (this.parent) {
                this.parent.grid = value
                this.parent.lookupClose()
              }
            });
          }
          break
        }
        // Button Sort (Lookup)
        case GridCellEnum.ButtonLookupSort: {
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
        this.lookupGrid.parentCell = cell
        this.serverApi.commandGridLoad(this.lookupGrid).subscribe(value => this.lookupGrid = value);
      }
    }
  }

  lookupClose() {
    this.lookupCell = undefined
    this.lookupGrid = undefined
  }
}
