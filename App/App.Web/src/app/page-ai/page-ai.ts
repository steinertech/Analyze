import { Component, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-ai',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-ai.html',
  styleUrl: './page-ai.css'
})
export class PageAi {
  @ViewChild('gridAi') gridAi!: PageGrid;

  async ngAfterViewInit() {
    await this.gridAi.load2('Ai')
  }
}
