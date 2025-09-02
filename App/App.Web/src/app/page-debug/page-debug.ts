import { Component, inject } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { PageNotification } from "../page-notification/page-notification";
import { NotificationEnum, NotificationService } from '../notification.service';

@Component({
  selector: 'app-page-debug',
  imports: [PageNav, PageNotification],
  templateUrl: './page-debug.html',
  styleUrl: './page-debug.css'
})
export class PageDebug {
  private notificationService = inject(NotificationService)
  
  click() {
    this.notificationService.add(NotificationEnum.Info, "Hello Notification");
  }
}
