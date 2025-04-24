import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { GridCellDto, GridDto } from '../generate';
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
  @Input() grid?: GridDto
  
  click() {
    if (this.grid) {
      this.grid.rowCellList.push([{ cellEnum: 0 }])
    }
  }

  cellTextChange(cell: GridCellDto, value: string) {
    cell.textModified = value
  }
}
