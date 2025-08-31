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

  ngAfterContentInit() {
    if (this.serverApi.isWindow()) {
      this.serverApi.commandGridLoad(this.grid()).subscribe(value => this.grid.set(value.grid));
    }
  }
}
