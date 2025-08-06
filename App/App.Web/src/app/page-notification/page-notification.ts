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
    this.dataService.notificationList = this.dataService.notificationList.filter(item => item != value)
  }
}
