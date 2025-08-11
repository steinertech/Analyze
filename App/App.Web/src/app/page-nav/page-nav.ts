import { Component, signal } from '@angular/core';
import { BreakpointObserver, LayoutModule } from '@angular/cdk/layout';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-page-nav',
  imports: [RouterModule, LayoutModule],
  templateUrl: './page-nav.html',
  styleUrl: './page-nav.css'
})
export class PageNav {
  constructor(observer: BreakpointObserver) {
    observer.observe(['(max-width: 640px)']).subscribe(result => { // See also https://v2.tailwindcss.com/docs/responsive-design
      if (!result.matches) {
        // User increased window size over break point
        this.isShow.set(false);
      }
    })
  }

  isShow = signal(false);
  
  click() {
    this.isShow.set(!this.isShow());
  }
}
