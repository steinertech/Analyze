import { Component } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { DataService, NotificationEnum } from '../data.service';

@Component({
  selector: 'app-page-debug',
  imports: [PageNav],
  templateUrl: './page-debug.html',
  styleUrl: './page-debug.css'
})
export class PageDebug {
  constructor(private dataService : DataService) {
  }
  
  click() {
    this.dataService.notificationAdd(NotificationEnum.Info, "Hello Notification");
  }
}
