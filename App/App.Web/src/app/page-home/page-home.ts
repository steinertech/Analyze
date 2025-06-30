import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { PageNotification } from '../page-notification/page-notification';

@Component({
  selector: 'app-page-home',
  imports: [
    PageNav,
    PageNotification
  ],
  templateUrl: './page-home.html',
  styleUrl: './page-home.css'
})
export class PageHome {

}
