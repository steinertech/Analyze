import { Component, inject } from '@angular/core';
import { NotificationDto, NotificationEnum, NotificationService } from '../notification.service';

@Component({
  selector: 'app-page-notification',
  imports: [],
  templateUrl: './page-notification.html',
  styleUrl: './page-notification.css'
})
export class PageNotification {
  protected notificationService = inject(NotificationService)

  NotificationEnum = NotificationEnum

  click(value: NotificationDto) {
    this.notificationService.list.update(list => {
      list = list.filter(item => item != value)
      return list
    })
  }
}
