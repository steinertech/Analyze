import { Routes } from '@angular/router';
import { PageHome } from './page-home/page-home';
import { PageAbout } from './page-about/page-about';

export const routes: Routes = [
  { path: '', component: PageHome },
  { path: 'about', component: PageAbout },
];
