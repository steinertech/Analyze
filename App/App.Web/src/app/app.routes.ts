import { Routes } from '@angular/router';
import { PageHome } from './page-home/page-home';
import { PageAbout } from './page-about/page-about';
import { PageDebug } from './page-debug/page-debug';
import { PageUser } from './page-user/page-user';
import { PageStorage } from './page-storage/page-storage';
import { PageProduct } from './page-product/page-product';
import { PageArticle } from './page-article/page-article';
import { PageOrganisation } from './page-organisation/page-organisation';
import { PageSchema } from './page-schema/page-schema';

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
  { path: 'product', component: PageProduct },
  { path: 'storage', component: PageStorage },
  { path: 'schema', component: PageSchema },
  { path: 'article', component: PageArticle },
  { path: 'organisation', component: PageOrganisation },
];
