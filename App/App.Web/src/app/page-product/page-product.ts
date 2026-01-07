import { AfterViewInit, ChangeDetectionStrategy, Component, ViewChild } from '@angular/core';
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
  @ViewChild('gridProduct') gridProduct!: PageGrid;

  async ngAfterViewInit() {
    await this.gridProduct.load2('ProductDto')
  }
}
