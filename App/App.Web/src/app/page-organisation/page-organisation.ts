import { Component, inject, ViewChild } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageGrid } from "../page-grid/page-grid";
import { ServerApi } from '../generate';

@Component({
  selector: 'app-page-organisation',
  imports: [PageNav, PageNotification, PageGrid],
  templateUrl: './page-organisation.html',
  styleUrl: './page-organisation.css'
})
export class PageOrganisation {
  @ViewChild('gridOrganisation') gridOrganisation!: PageGrid;
  @ViewChild('gridOrganisationEmail') gridOrganisationEmail!: PageGrid;

  async ngAfterViewInit() {
    await this.gridOrganisation.load2('Organisation')
    await this.gridOrganisationEmail.load2('OrganisationEmail')
  }
}
