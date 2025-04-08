import { Routes } from '@angular/router';
import { PageHomeComponent } from './page-home/page-home.component';
import { PageDebugComponent } from './page-debug/page-debug.component';

export const routes: Routes = [
  { path: '', component: PageHomeComponent },
  { path: 'debug', component: PageDebugComponent },
];
