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
      case GridCellEnum.Header: {
        return this.grid?.state?.filterList?.find(value => value.fieldName == cell.fieldName)?.text
      }
      case GridCellEnum.Field: {
        return cell.textModified ?? cell.text
      }
    }
    return undefined
  }

  cellTextSet(cell: GridCellDto, value: string) {
    if (this.grid) {
      switch (cell.cellEnum) {
        case GridCellEnum.Header: {
          if (!this.grid.state) {
            this.grid.state = {}
          }
          if (!this.grid.state.filterList) {
            this.grid.state.filterList = []
          }
          let index = this.grid.state.filterList.findIndex(value => value.fieldName == cell.fieldName)
          if (index == -1) {
            index = this.grid.state.filterList.push({ fieldName: cell.fieldName!, text: '' }) - 1 // Add
          }
          if (value == '') {
            this.grid.state.filterList.splice(index, 1) // Remove
          } else {
            this.grid.state.filterList[index].text = value
          }
          break
        }
        case GridCellEnum.Field: {
          cell.textModified = value
          break
        }
      }
    }
  }

  click(cell: GridCellDto) {
    if (this.grid) {
      switch (cell.cellEnum) {
        case GridCellEnum.Header: {
          break
        }
        case GridCellEnum.ButtonCancel: {
          this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value); // Reload
          break
        }
        case GridCellEnum.ButtonSave: {
          this.serverApi.commandGridSave(this.grid).subscribe(value => this.grid = value);
          break
        }
        case GridCellEnum.ButtonLookupCancel: {
          if (this.parent) {
            this.parent.lookupCell!.text = 'Lookup Cancel'
            this.parent.lookupClose()
          }
          break
        }
        case GridCellEnum.ButtonLookupOk: {
          if (this.parent) {
            this.parent.lookupCell!.text = 'Lookup Ok'
            this.parent.lookupClose()
          }
          break
        }
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
