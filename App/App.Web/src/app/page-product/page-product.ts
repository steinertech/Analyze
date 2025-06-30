import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { GridDto, ServerApi } from '../generate';
import { DataService } from '../data.service';
import { PageGrid } from '../page-grid/page-grid';
import { PageNav } from '../page-nav/page-nav';

@Component({
  selector: 'app-page-product',
  imports: [
    PageNav,
    PageGrid
  ],
  templateUrl: './page-product.html',
  styleUrl: './page-product.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageProduct {
  constructor(private serverApi: ServerApi, private dataService: DataService) {
  }

  readonly grid = signal<GridDto>({ gridName: 'ProductDto' })
  readonly gridExcel = signal<GridDto>({ gridName: 'Excel' })
  readonly gridStorage = signal<GridDto>({ gridName: 'Storage' })

  ngAfterContentInit() {
    if (this.dataService.isWindow()) {
      this.serverApi.commandGridLoad(this.grid()).subscribe(value => this.grid.set(value));
      // this.serverApi.commandGridLoad(this.gridExcel).subscribe(value => this.gridExcel = value);
      this.serverApi.commandGridLoad(this.gridStorage()).subscribe(value => this.gridStorage.set(value));
    }
  }
}
