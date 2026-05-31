import { Component, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-job',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-job.html',
  styleUrl: './page-job.css'
})
export class PageJob {
  @ViewChild('grid') grid!: PageGrid;
  async ngAfterViewInit() {
    await this.grid.load2('Job')
  }
}
