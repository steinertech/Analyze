import { Component } from '@angular/core';
import { PageGridComponent } from "../page-grid/page-grid.component";
import { GridDto, ServerApi } from '../generate';
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

  grid?: GridDto

  clickLoad() {
    this.serverApi.commandGridLoad('ProductDto').subscribe(value => this.grid = value);
  }

  clickSave() {
    if (this.grid) {
      this.serverApi.commandGridSave(this.grid).subscribe();
    }
  }
}
