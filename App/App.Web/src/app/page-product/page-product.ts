import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { GridDto, ServerApi } from '../generate';
import { DataService } from '../data.service';
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
export class PageProduct {
  constructor(private serverApi: ServerApi, private dataService: DataService) {
  }

  readonly grid = signal<GridDto>({ gridName: 'ProductDto' })
  readonly gridExcel = signal<GridDto>({ gridName: 'Excel' })
  readonly gridStorage = signal<GridDto>({ gridName: 'Storage' })

  async ngAfterContentInit() {
    if (this.serverApi.isWindow()) {
      const load = await this.serverApi.commandGridLoad(this.grid())
      this.grid.set(load.grid)
      // this.serverApi.commandGridLoad(this.gridExcel).subscribe(value => this.gridExcel = value);
      const loadStorage = await this.serverApi.commandGridLoad(this.gridStorage())
      this.gridStorage.set(loadStorage.grid)
    }
  }
}
