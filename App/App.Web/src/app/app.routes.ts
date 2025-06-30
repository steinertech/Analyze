import { Routes } from '@angular/router';
import { PageHome } from './page-home/page-home';
import { PageAbout } from './page-about/page-about';
import { PageDebug } from './page-debug/page-debug';
import { PageUser } from './page-user/page-user';
import { PageStorage } from './page-storage/page-storage';
import { PageProduct } from './page-product/page-product';

export const routes: Routes = [
  { path: '', component: PageHome },
  { path: 'debug', component: PageDebug },
  { path: 'about', component: PageAbout },
  { path: 'signin', component: PageUser },
  { path: 'signout', component: PageUser },
  { path: 'signup', component: PageUser },
  { path: 'signup-email', component: PageUser },
  { path: 'signup-confirm', component: PageUser },
  { path: 'signin-recover', component: PageUser },
  { path: 'signin-password-change', component: PageUser },
  { path: 'storage', component: PageStorage },
  { path: 'product', component: PageProduct },  
];
