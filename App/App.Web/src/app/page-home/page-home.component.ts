import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { PageNotificationComponent } from '../page-notification/page-notification.component';
import { DataService, NotificationEnum } from '../data.service';

@Component({
  selector: 'app-page-home',
  imports: [PageNavComponent, PageNotificationComponent],
  templateUrl: './page-home.component.html',
  styleUrl: './page-home.component.css'
})
export class PageHomeComponent {
  constructor(private dataService : DataService) {
  }

  click(notificationEnum: NotificationEnum) {
    this.dataService.notificationAdd(notificationEnum, "Hello Notification");
  }
}
