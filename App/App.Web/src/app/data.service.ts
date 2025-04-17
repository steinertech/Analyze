import { Injectable, Injector } from '@angular/core';
import { ServerApi } from './generate';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataService {
  constructor(private injector: Injector) {
  }

  public isWindow() {
    return typeof window !== "undefined";
  }

  private isLocalhost() {
    let result = false
    if (this.isWindow()) {
      let hostname = window.location.hostname
      result =
        hostname == "localhost" || // Running in VS code
        hostname == '127.0.0.1' // Running with http-server (ng build --localize)
    }
    return result
  }

  public serverUrl() {
    let result = "https://stc001appfunction.azurewebsites.net/api/data"
    if (this.isLocalhost()) {
      result = "http://localhost:7138/api/data";
    }
    return result
  }

  storageDownloadList = new Map<string, Observable<string>>(); // (FileName, Data)
  storageDownloadEmpty = of(""); // Used for SSR
  public storageDownload(fileName: string) {
    let result = this.storageDownloadList.get(fileName)
    if (!result) {
      console.log("Get File", fileName)
      if (this.isWindow()) {
        let serverApi = this.injector.get(ServerApi) // Circular dependency
        this.storageDownloadList.set(fileName, serverApi.commandStorageDownload(fileName))
      } else {
        this.storageDownloadList.set(fileName, this.storageDownloadEmpty);
      }
    }
    result = this.storageDownloadList.get(fileName)
    return result
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
