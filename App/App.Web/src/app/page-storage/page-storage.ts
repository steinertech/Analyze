import { Component, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-storage',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-storage.html',
  styleUrl: './page-storage.css'
})
export class PageStorage {
  @ViewChild('gridStorage') gridStorage!: PageGrid;
  @ViewChild('gridExcel') gridExcel!: PageGrid;

  async ngAfterViewInit() {
      await this.gridExcel.load2('Excel', false)
      await this.gridStorage.load2('Storage')
  }
}
