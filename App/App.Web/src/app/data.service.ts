import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  constructor() { 

  }


  notificationList: NotificationDto[] = [];
  notificationCount: number = 0;
  notificationAdd(notificationEnum: NotificationEnum, text: string) {
    let notification = new NotificationDto();
    notification.id = this.notificationCount++;
    notification.notificationEnum = notificationEnum;
    notification.text = text;
    this.notificationList.push(notification);
  }

  isSignin = false;
}

export enum NotificationEnum {
  None = 0,
  Success = 1,
  Info = 2,
  Warning = 3,
  Error = 4,
}

class NotificationDto {
  id?: number;
  notificationEnum?: number;
  text?: string
}

