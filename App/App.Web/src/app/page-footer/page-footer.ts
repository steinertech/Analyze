import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-page-footer',
  imports: [
    RouterLink
  ],
  templateUrl: './page-footer.html',
  styleUrl: './page-footer.css'
})
export class PageFooter {
  readonly year = new Date().getFullYear();
}
