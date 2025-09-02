import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  list = signal([] as NotificationDto[]);
  add(notificationEnum: NotificationEnum, text: string) {
    const notification = new NotificationDto();
    notification.notificationEnum = notificationEnum;
    notification.text = text;
    this.list.update(value => {
      value = [notification, ...value]
      return value
    })
  }
}

export enum NotificationEnum {
  None = 0,
  Info = 1,
  Success = 2,
  Warning = 3,
  Error = 4,
}

export class NotificationDto {
  notificationEnum?: NotificationEnum;
  text?: string
}