import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';

@Component({
  selector: 'app-page-about',
  imports: [PageNav],
  templateUrl: './page-about.html',
  styleUrl: './page-about.css'
})
export class PageAbout {
  text = $localize`:@@debugKeyTs:Hello Ts (Native)`
  click() {
  }
}
