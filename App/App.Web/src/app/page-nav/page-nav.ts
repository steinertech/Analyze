import { Component, inject, signal, LOCALE_ID } from '@angular/core';
import { BreakpointObserver, LayoutModule } from '@angular/cdk/layout';
import { Router, RouterModule } from '@angular/router';
import { DataService } from '../data.service';
import { ServerApi } from '../server-api';

@Component({
  selector: 'app-page-nav',
  imports: [RouterModule, LayoutModule],
  templateUrl: './page-nav.html',
  styleUrl: './page-nav.css'
})
export class PageNav {
  private observer = inject(BreakpointObserver)
  private router = inject(Router)
  protected dataService = inject(DataService)
  protected serverApi = inject(ServerApi)
  protected localeId = inject(LOCALE_ID)

  get langSwitchUrl(): string {
    return this.localeId === 'de' ? this.router.url : '/de' + this.router.url;
  }

  get langSwitchLabel(): string {
    return this.localeId === 'de' ? 'EN' : 'DE';
  }

  constructor() {
    this.observer.observe(['(max-width: 640px)']).subscribe(result => { // See also https://v2.tailwindcss.com/docs/responsive-design
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
