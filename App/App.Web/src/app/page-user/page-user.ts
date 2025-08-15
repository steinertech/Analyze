import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ServerApi, UserDto } from '../generate';
import { PageNotification } from "../page-notification/page-notification";
import { DataService } from '../data.service';

@Component({
  selector: 'app-page-user',
  imports: [
    PageNav,
    RouterModule,
    FormsModule,
    PageNotification
  ],
  templateUrl: './page-user.html',
  styleUrl: './page-user.css'
})
export class PageUser {
  constructor(private serverApi: ServerApi, private router: Router, protected dataService: DataService) {
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
      case "/signin-dashboard": this.userModeEnum = UserModeEnum.SignInDashboard; break;
    }
  }

  async ngAfterContentInit() {
    if (this.userModeEnum == UserModeEnum.SignOut) {
      if (this.dataService.isWindow()) {
        await this.serverApi.commmandUserSignOut()
        await this.dataService.userUpdate()
      }
    }
  }

  userEmail?: string;
  userPassword?: string;
  userPasswordConfirm?: string;
  async click() {
    if (this.userModeEnum == UserModeEnum.SignIn) {
      await this.serverApi.commmandUserSignIn(<UserDto>{ email: this.userEmail, password: this.userPassword })
      await this.dataService.userUpdate()
    }
    if (this.userModeEnum == UserModeEnum.SignUp) {
      await this.serverApi.commmandUserSignUp(<UserDto>{ email: this.userEmail, password: this.userPassword })
    }
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
  SignInDashboard = 8 // Page shown after user sign in
}