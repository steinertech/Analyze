import { AfterContentInit, Component, inject, OnInit } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ServerApi } from '../generate';
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
export class PageUser implements OnInit, AfterContentInit {
  private serverApi = inject(ServerApi)
  private router = inject(Router)
  protected dataService = inject(DataService)

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

  async ngAfterContentInit() {
    if (this.userModeEnum == UserModeEnum.SignOut) {
      if (this.serverApi.isWindow()) {
        await this.serverApi.commmandUserSignOut()
        await this.dataService.userSignUpdate()
      }
    }
  }

  userEmail?: string;
  userPassword?: string;
  userPasswordConfirm?: string;
  async click() {
    if (this.userModeEnum == UserModeEnum.SignIn) {
      // SignIn
      await this.serverApi.commmandUserSignIn({ email: this.userEmail, password: this.userPassword })
      await this.dataService.userSignUpdate()
      if (this.dataService.userSign()) {
        this.serverApi.navigate('/')
      }
    }
    if (this.userModeEnum == UserModeEnum.SignUp) {
      // SignUp
      await this.serverApi.commmandUserSignUp({ email: this.userEmail, password: this.userPassword })
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
}