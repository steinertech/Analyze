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

  @Input() grid?: GridDto

  @Input() gridLookup?: GridDto

  GridCellEnum = GridCellEnum

  isLookup?: GridCellDto

  cellTextGet(cell: GridCellDto) {
    if (cell.cellEnum == GridCellEnum.Header) {
      return undefined
    }
    return cell.textModified ?? cell.text
  }

  cellTextSet(cell: GridCellDto, value: string) {
    cell.textModified = value
  }

  clickCancel() {
    if (this.grid) {
      this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value);
    }
  }

  clickSave() {
    if (this.grid) {
      this.serverApi.commandGridSave(this.grid).subscribe(value => this.grid = value);
    }
  }

  clickLookup(cell: GridCellDto) {
    if (this.grid) {
      if (this.isLookup == cell) {
        this.isLookup = undefined // Lookup close
      } else {
        this.isLookup = cell // Lookup open
      }
      this.gridLookup = undefined // Clear data
      if (this.isLookup) {
        this.gridLookup = { gridName: this.grid?.gridName }
        this.serverApi.commandGridLoad(this.gridLookup).subscribe(value => this.gridLookup = value);
      }
    }
  }
}
