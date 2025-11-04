import { AfterViewInit, Component, inject, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { ServerApi } from '../generate';
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
export class PageArticle implements AfterViewInit {
  private serverApi = inject(ServerApi)

  @ViewChild('gridArticle') gridArticle!: PageGrid;
  
  @ViewChild('gridArticle2') gridArticle2!: PageGrid;
  
  @ViewChild('gridArticle3') gridArticle3!: PageGrid;

  async click() {
    this.gridArticle2.load2('Article2')
  }

  async ngAfterViewInit() {
    if (this.serverApi.isWindow()) {
      await Promise.all([
        this.gridArticle.load2('Article'),
        this.gridArticle2.load2('Article2'),
        this.gridArticle3.load2('Article3'),
      ])
    }
  }
}
