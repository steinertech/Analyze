import { Routes } from '@angular/router';
import { PageHomeComponent } from './page-home/page-home.component';
import { PageDebugComponent } from './page-debug/page-debug.component';
import { PageAboutComponent } from './page-about/page-about.component';
import { PageUserComponent } from './page-user/page-user.component';

export const routes: Routes = [
  { path: '', component: PageHomeComponent },
  { path: 'debug', component: PageDebugComponent },
  { path: 'about', component: PageAboutComponent },
  { path: 'signin', component: PageUserComponent },
  { path: 'signout', component: PageUserComponent },
  { path: 'signup', component: PageUserComponent },
  { path: 'recover', component: PageUserComponent },
];
