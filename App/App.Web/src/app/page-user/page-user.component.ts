import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-page-user',
  imports: [PageNavComponent, RouterModule],
  templateUrl: './page-user.component.html',
  styleUrl: './page-user.component.css'
})
export class PageUserComponent {

}
