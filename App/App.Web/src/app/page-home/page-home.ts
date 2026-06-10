import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { PageNotification } from '../page-notification/page-notification';
import { PageFooter } from "../page-footer/page-footer";

@Component({
  selector: 'app-page-home',
  imports: [
    PageNav,
    PageNotification,
    PageFooter
],
  templateUrl: './page-home.html',
  styleUrl: './page-home.css'
})
export class PageHome {
  readonly year = new Date().getFullYear();
}
