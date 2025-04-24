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

  GridCellEnum = GridCellEnum

  cellTextGet(cell: GridCellDto) {
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
}
