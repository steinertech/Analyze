import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { AppServerCommand } from '../generate';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-page-about',
  imports: [
    PageNavComponent, 
    CommonModule // Async pipe
  ],
  templateUrl: './page-about.component.html',
  styleUrl: './page-about.component.css'
})
export class PageAboutComponent {
  constructor(public server: AppServerCommand) {
  }

  click() {
    this.server.CommandVersion().subscribe(v => console.log("Version", v))
  }
}
