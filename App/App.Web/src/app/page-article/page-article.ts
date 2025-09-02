import { Component, signal } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { GridDto, ServerApi } from '../generate';
import { PageGrid } from "../page-grid/page-grid";
import { JsonPipe } from '@angular/common';

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
export class PageArticle {
  constructor(private serverApi: ServerApi) {
    serverApi.commmandUserSignOut
  }

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
