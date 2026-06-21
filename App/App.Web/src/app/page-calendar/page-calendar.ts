import { Component, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-calendar',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-calendar.html',
  styleUrl: './page-calendar.css'
})
export class PageCalendar {
  @ViewChild('grid') grid!: PageGrid;
}
