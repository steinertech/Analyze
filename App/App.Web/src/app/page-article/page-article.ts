import { Component } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { ServerApi } from '../generate';

@Component({
  selector: 'app-page-article',
  imports: [PageNav, PageNotification],
  templateUrl: './page-article.html',
  styleUrl: './page-article.css'
})
export class PageArticle {
  constructor(private serverApi: ServerApi) {
    serverApi.commmandUserSignOut
  }

  click() {
    this.serverApi.commmandArticleAdd()
  }
}
