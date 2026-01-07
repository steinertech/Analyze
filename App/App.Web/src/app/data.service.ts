import { inject, Injectable, signal } from '@angular/core';
import { ServerApi, UserStatusDto } from './generate';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  private serverApi = inject(ServerApi)

  constructor() {
    if (this.serverApi.isBrowser()) {
      this.userSignStatusUpdate()
    }
  }

  storageDownloadList = new Map<string, Observable<string>>(); // (FileName, Data)
  storageDownloadEmpty = of(""); // Used for SSR
  public storageDownload(fileName: string) {
    let result = this.storageDownloadList.get(fileName)
    if (!result) {
      console.log("Get File", fileName)
      if (this.serverApi.isBrowser()) {
        const serverApi = this.serverApi
        this.storageDownloadList.set(fileName, serverApi.commandStorageDownload(fileName))
      } else {
        this.storageDownloadList.set(fileName, this.storageDownloadEmpty);
      }
    }
    result = this.storageDownloadList.get(fileName)
    return result
  }

  /** Currently signed in in user  */
  userSignStatus = signal<UserStatusDto | undefined>(undefined)
  async userSignStatusUpdate() {
    const serverApi = this.serverApi
    const userSign = await serverApi.commmandUserSignStatus()
    this.userSignStatus.set(userSign)
  }
}

