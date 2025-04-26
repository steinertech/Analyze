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
    return cell.textModified ?? cell.text
  }

  cellTextSet(cell: GridCellDto, value: string) {
    cell.textModified = value
  }

  click(cell: GridCellDto) {
    if (this.grid) {
      switch (cell.cellEnum) {
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
