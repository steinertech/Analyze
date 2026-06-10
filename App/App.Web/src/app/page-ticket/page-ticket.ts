import { Component, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";

@Component({
  selector: 'app-page-ticket',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-ticket.html',
  styleUrl: './page-ticket.css'
})
export class PageTicket {
  @ViewChild('grid') grid!: PageGrid;
}
