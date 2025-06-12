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
  gridExcel: GridDto = { gridName: 'Excel' }
  gridStorage: GridDto = { gridName: 'Storage' }

  ngAfterContentInit() {
    if (this.dataService.isWindow()) {
      this.serverApi.commandGridLoad(this.grid).subscribe(value => this.grid = value);
      // this.serverApi.commandGridLoad(this.gridExcel).subscribe(value => this.gridExcel = value);
      this.serverApi.commandGridLoad(this.gridStorage).subscribe(value => this.gridStorage = value);
    }
  }
}
