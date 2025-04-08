import { Component } from '@angular/core';

@Component({
  selector: 'app-page-debug',
  imports: [],
  templateUrl: './page-debug.component.html',
  styleUrl: './page-debug.component.css'
})
export class PageDebugComponent {
  text = $localize`:@@debugKeyTs:Hello Ts (Native)`
  click() {
  }
}
