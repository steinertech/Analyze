import { Component } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";

@Component({
  selector: 'app-page-organisation',
  imports: [PageNav, PageNotification],
  templateUrl: './page-organisation.html',
  styleUrl: './page-organisation.css'
})
export class PageOrganisation {

}
