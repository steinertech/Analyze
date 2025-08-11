import { Component } from '@angular/core';
import { DataService, NotificationDto, NotificationEnum } from '../data.service';

@Component({
  selector: 'app-page-notification',
  imports: [],
  templateUrl: './page-notification.html',
  styleUrl: './page-notification.css'
})
export class PageNotification {
  constructor(public dataService: DataService) {
  }

  NotificationEnum = NotificationEnum

  click(value: NotificationDto) {
    this.dataService.notificationList.update(list => {
      list = list.filter(item => item != value)
      return list
    })
  }
}
