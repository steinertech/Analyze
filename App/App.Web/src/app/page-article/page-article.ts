import { AfterContentInit, Component, inject, signal } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { GridDto, ServerApi } from '../generate';
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-article',
  imports: [
    PageNav,
    PageNotification,
    PageGrid,
    // JsonPipe
  ],
  templateUrl: './page-article.html',
  styleUrl: './page-article.css'
})
export class PageArticle implements AfterContentInit {
  private serverApi = inject(ServerApi)

  readonly grid = signal<GridDto>({ gridName: 'Article' })

  readonly grid2 = signal<GridDto>({ gridName: 'Article2' })

  async ngAfterContentInit() {
    if (this.serverApi.isWindow()) {
      const [load, load2] = await Promise.all([
        this.serverApi.commandGridLoad(this.grid()),
        this.serverApi.commandGridLoad(this.grid2())
      ])
      this.grid.set(load.grid);
      this.grid2.set(load2.grid);
    }
  }
}
