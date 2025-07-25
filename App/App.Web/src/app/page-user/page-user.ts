import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ServerApi, UserDto } from '../generate';

@Component({
  selector: 'app-page-user',
  imports: [
    PageNav,
    RouterModule,
    FormsModule, // Used for ngModels    
  ],
  templateUrl: './page-user.html',
  styleUrl: './page-user.css'
})
export class PageUser {
  constructor(private serverApi: ServerApi, private router: Router) {
  }

  userModeEnum: UserModeEnum = UserModeEnum.None

  UserModeEnumLocal = UserModeEnum

  ngOnInit() {
    switch (this.router.url) {
      case "/signin": this.userModeEnum = UserModeEnum.SignIn; break;
      case "/signout": this.userModeEnum = UserModeEnum.SignOut; break;
      case "/signup": this.userModeEnum = UserModeEnum.SignUp; break;
      case "/signup-email": this.userModeEnum = UserModeEnum.SignUpEmail; break;
      case "/signup-confirm": this.userModeEnum = UserModeEnum.SignUpConfirm; break;
      case "/signin-recover": this.userModeEnum = UserModeEnum.SignInRecover; break;
      case "/signin-password-change": this.userModeEnum = UserModeEnum.SignInPasswordChange; break;
    }
  }

  userEmail?: string;
  userPassword?: string;
  userPasswordConfirm?: string;
  click() {
    this.serverApi.commmandUserSignUp(<UserDto>{ email: this.userEmail, password: this.userPassword }).subscribe();
  }
}

export enum UserModeEnum {
  None = 0,
  SignIn = 1,
  SignOut = 2,
  SignUp = 3,
  SignUpEmail = 4,
  SignUpConfirm = 5,
  SignInRecover = 6,
  SignInPasswordChange = 7,
}