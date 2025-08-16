import { Injectable, signal } from '@angular/core';
import { ServerApi, UserDto } from './generate';
import { Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataService {

  constructor(private serverApi: ServerApi) {
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
        let serverApi = this.serverApi
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
    let serverApi = this.serverApi
    let userSign = await serverApi.commmandUserSignStatus()
    this.userSign.set(userSign)
  }
}

