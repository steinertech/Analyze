import { Component } from '@angular/core';
import { PageGridComponent } from "../page-grid/page-grid.component";
import { GridDto, ServerApi } from '../generate';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { DataService } from '../data.service';

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
  constructor(private serverApi: ServerApi, private dataService: DataService) {
  }

  grid: GridDto = { gridName: 'ProductDto' }

  ngAfterContentInit() {
    if (this.dataService.isWindow()) {
      this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value);
    }
  }
}
