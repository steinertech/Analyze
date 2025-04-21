import { CommonModule } from '@angular/common';
import { Component, Input, input } from '@angular/core';
import { GridConfigDto } from '../generate';

@Component({
  selector: 'app-page-grid',
  imports: [
    CommonModule, // Json pipe
  ],
  templateUrl: './page-grid.component.html',
  styleUrl: './page-grid.component.css'
})
export class PageGridComponent {
  @Input() rowList?: any[]
  @Input() gridConfig?: GridConfigDto
}
