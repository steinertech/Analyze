import { Component } from '@angular/core';
import { BreakpointObserver, LayoutModule } from '@angular/cdk/layout';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-page-nav',
  imports: [
    RouterModule,
    LayoutModule
  ],
  templateUrl: './page-nav.component.html',
  styleUrl: './page-nav.component.css'
})
export class PageNavComponent {
  constructor(observer: BreakpointObserver) {
    observer.observe(['(max-width: 640px)']).subscribe(result => { // See also https://v2.tailwindcss.com/docs/responsive-design
      if (result.matches) {
        this.isOpen = true;
      }
    })
  }

  isOpen = false;
  click() {
    this.isOpen = !this.isOpen;
  }
}
