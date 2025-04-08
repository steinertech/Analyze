import { Routes } from '@angular/router';
import { PageHomeComponent } from './page-home/page-home.component';
import { PageDebugComponent } from './page-debug/page-debug.component';
import { PageProjectComponent } from './page-project/page-project.component';
import { PageAboutComponent } from './page-about/page-about.component';

export const routes: Routes = [
  { path: '', component: PageHomeComponent },
  { path: 'debug', component: PageDebugComponent },
  { path: 'project', component: PageProjectComponent },
  { path: 'about', component: PageAboutComponent },
];
