import { inject, Injectable, signal } from '@angular/core';
import { ServerApi, UserDto } from './generate';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  private serverApi = inject(ServerApi)

  constructor() {
    if (this.serverApi.isWindow()) {
      this.userSignUpdate()
    }
  }

  storageDownloadList = new Map<string, Observable<string>>(); // (FileName, Data)
  storageDownloadEmpty = of(""); // Used for SSR
  public storageDownload(fileName: string) {
    let result = this.storageDownloadList.get(fileName)
    if (!result) {
      console.log("Get File", fileName)
      if (this.serverApi.isWindow()) {
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
  userSign = signal<UserDto | undefined>(undefined)
  async userSignUpdate() {
    const serverApi = this.serverApi
    const userSign = await serverApi.commmandUserSignStatus()
    this.userSign.set(userSign)
  }
}

