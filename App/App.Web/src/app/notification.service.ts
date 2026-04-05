import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  /** List of notifications displayed on app. */
  list = signal([] as NotificationDto[]);
  
  /** Add new notification. */
  add(notificationEnum: NotificationEnum, text: string) {
    const notification = new NotificationDto();
    notification.notificationEnum = notificationEnum;
    notification.text = text;
    this.list.update(value => {
      value = [notification, ...value]
      return value
    })
  }
  cacheCount = signal<number | undefined>(undefined)
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