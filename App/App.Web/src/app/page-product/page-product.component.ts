import { Component } from '@angular/core';
import { PageGridComponent } from "../page-grid/page-grid.component";
import { GridConfigDto, ServerApi } from '../generate';
import { PageNavComponent } from '../page-nav/page-nav.component';

@Component({
  selector: 'app-page-product',
  imports: [
    PageGridComponent,
    PageNavComponent
  ],
  templateUrl: './page-product.component.html',
  styleUrl: './page-product.component.css'
})
export class PageProductComponent {
  constructor(public serverApi: ServerApi) {
  }

  rowList?: any[]

  gridConfig?: GridConfigDto

  click() {
    this.serverApi.commandGridSelect('ProductDto').subscribe(value => this.rowList = value);
    this.serverApi.commandGridSelectConfig('ProductDto').subscribe(value => this.gridConfig = value);
  }
}
