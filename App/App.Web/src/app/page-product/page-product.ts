import { AfterViewInit, ChangeDetectionStrategy, Component, inject, signal, ViewChild } from '@angular/core';
import { GridDto, ServerApi } from '../generate';
import { PageGrid } from '../page-grid/page-grid';
import { PageNav } from '../page-nav/page-nav';
import { PageNotification } from "../page-notification/page-notification";

@Component({
  selector: 'app-page-product',
  imports: [
    PageNav,
    PageGrid,
    PageNotification
  ],
  templateUrl: './page-product.html',
  styleUrl: './page-product.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageProduct implements AfterViewInit {
  private serverApi = inject(ServerApi)

  @ViewChild('gridProduct') gridProduct!: PageGrid;
  @ViewChild('gridExcel') gridExcel!: PageGrid;
  @ViewChild('gridStorage') gridStorage!: PageGrid;

  async ngAfterViewInit() {
    if (this.serverApi.isWindow()) {
      await this.gridProduct.load2('ProductDto')
      // await this.gridExcel.load2('Excel')
      await this.gridStorage.load2('Storage')
    }
  }
}
