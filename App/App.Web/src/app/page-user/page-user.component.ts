import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ServerApi, UserDto } from '../generate';

@Component({
  selector: 'app-page-user',
  imports: [
    PageNavComponent,
    RouterModule,
    FormsModule // Used for ngModels
  ],
  templateUrl: './page-user.component.html',
  styleUrl: './page-user.component.css'
})
export class PageUserComponent {
  constructor(private serverApi: ServerApi) {
  }

  userEmail?: string = "E"
  userPassword?: string = "P"
  click() {
    this.serverApi.CommmandUserSignUp(<UserDto>{ email: this.userEmail, password: this.userPassword }).subscribe()
  }
}
