import { Routes } from '@angular/router';
import { PageHomeComponent } from './page-home/page-home.component';
import { PageDebugComponent } from './page-debug/page-debug.component';
import { PageAboutComponent } from './page-about/page-about.component';
import { PageUserComponent } from './page-user/page-user.component';
import { PageStorageComponent } from './page-storage/page-storage.component';

export const routes: Routes = [
  { path: '', component: PageHomeComponent },
  { path: 'debug', component: PageDebugComponent },
  { path: 'about', component: PageAboutComponent },
  { path: 'signin', component: PageUserComponent },
  { path: 'signout', component: PageUserComponent },
  { path: 'signup', component: PageUserComponent },
  { path: 'signup-email', component: PageUserComponent },
  { path: 'signup-confirm', component: PageUserComponent },
  { path: 'signin-recover', component: PageUserComponent },
  { path: 'signin-password-change', component: PageUserComponent },
  { path: 'storage', component: PageStorageComponent },
];
