import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { DataService, NotificationEnum } from '../data.service';

@Component({
  selector: 'app-page-debug',
  imports: [PageNavComponent],
  templateUrl: './page-debug.component.html',
  styleUrl: './page-debug.component.css'
})
export class PageDebugComponent {
  constructor(private dataService : DataService) {
  }

  click() {
    this.dataService.notificationAdd(NotificationEnum.Info, "Hello Notification");
  }
}
