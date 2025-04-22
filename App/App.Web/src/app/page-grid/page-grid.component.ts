import { CommonModule } from '@angular/common';
import { Component, Input, input } from '@angular/core';
import { GridCellDto, GridConfigDto, GridDto } from '../generate';

@Component({
  selector: 'app-page-grid',
  imports: [
    CommonModule, // Json pipe
  ],
  templateUrl: './page-grid.component.html',
  styleUrl: './page-grid.component.css'
})
export class PageGridComponent {
  gridDto?: GridDto
  @Input() rowList?: any[]
  @Input() gridConfig?: GridConfigDto
  click() {
    this.gridConfig?.grid?.cellList?.push([{ cellEnum: 0 }])
  }
}
