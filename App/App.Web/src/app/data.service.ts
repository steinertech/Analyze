import { Injectable, Injector, signal } from '@angular/core';
import { ServerApi, UserDto } from './generate';
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

  private serverApi() {
    return this.injector.get(ServerApi) // Circular dependency // TODO new data service Notification
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

  private isLocalhostGitHubCodeSpace() {
    let result = false
    if (this.isWindow()) {
      let hostname = window.location.hostname
      result =
        hostname.endsWith('github.dev') // Running on GitHub CodeSpace
    }
    return result
  }

  public serverUrl() {
    let result = "https://api.t2sync.com/api/data" // "https://stc001appfunction.azurewebsites.net/api/data" // TODO generic
    if (this.isLocalhost()) {
      result = "http://localhost:7138/api/data";
    }
    if (this.isLocalhostGitHubCodeSpace()) {
      result = 'https://' + window.location.hostname.replace('4200', '7138') + '/api/data'
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
        let serverApi = this.serverApi()
        this.storageDownloadList.set(fileName, serverApi.commandStorageDownload(fileName))
      } else {
        this.storageDownloadList.set(fileName, this.storageDownloadEmpty);
      }
    }
    result = this.storageDownloadList.get(fileName)
    return result
  }

  notificationList = signal(<NotificationDto[]>[]);
  notificationCount: number = 0;
  notificationAdd(notificationEnum: NotificationEnum, text: string) {
    let notification = new NotificationDto();
    notification.notificationEnum = notificationEnum;
    notification.text = text;
    this.notificationList.update(list => {
      list = [notification, ...list]
      return list
    })
  }

  /** Currently loggen in user  */
  user = signal<UserDto | undefined>(undefined)
  async userUpdate() {
    let serverApi = this.serverApi()
    let user = await serverApi.commmandUserSignStatus()
    this.user.set(user)
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