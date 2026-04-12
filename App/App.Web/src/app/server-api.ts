import { HttpClient, HttpHeaders } from "@angular/common/http"
import { inject, Injectable, PLATFORM_ID, signal } from "@angular/core"
import { catchError, firstValueFrom, map, mergeMap, Observable, of, tap } from "rxjs"
import { Router } from "@angular/router"
import { NotificationEnum, NotificationService } from "./notification.service"
import { UtilClient } from "./util-client"
import { isPlatformBrowser } from "@angular/common"
import { ComponentDto, GridRequest2Dto, GridRequestDto, GridResponse2Dto, RequestDto, ResponseDto, UserDto, UserStatusDto } from "./generate"

@Injectable({
  providedIn: 'root',
})
export class ServerApi {
  private platformId = inject(PLATFORM_ID)
  private httpClient = inject(HttpClient)
  private router = inject(Router)
  private notificationService = inject(NotificationService)

  public isBrowser() {
    return isPlatformBrowser(this.platformId)
  }

  private isLocalhost() {
    let result = false
    if (this.isBrowser()) {
      const hostname = window.location.hostname
      result =
        hostname == "localhost" || // Running in VS code
        hostname == '127.0.0.1' // Running with http-server (ng build --localize)
    }
    return result
  }

  private isLocalhostGitHubCodeSpace() {
    let result = false
    if (this.isBrowser()) {
      const hostname = window.location.hostname
      result =
        hostname.endsWith('github.dev') // Running on GitHub CodeSpace
    }
    return result
  }

  /** Returns configuration based on client domain. */
  private configuration() {
    const partList = window.location.hostname.split('.')
    partList[0] = 'api2' // www.example.com to api.example.com
    const urlApi = partList.join('.') + '/api/data'
    let result = { serverUrl: 'https://' + urlApi, isDevelopment: false }
    if (this.isLocalhost()) {
      result = { serverUrl: 'http://localhost:7138/api/data', isDevelopment: true }
    }
    if (this.isLocalhostGitHubCodeSpace()) {
      result = { serverUrl: 'https://' + window.location.hostname.replace('4200', '7138') + '/api/data', isDevelopment: true }
    }
    return result
  }

  public navigate(url: string) {
    this.router.navigateByUrl(url)
  }

  private postCount = 0
  private postCountAdd(value: number) {
    setTimeout(() => { // Prevent error Writing to signals is not allowed while Angular renders the template
      this.postCount += value
      this.isPost.set(this.postCount > 0)
    })
  }
  public isPost = signal(false)

  private post<T>(request: RequestDto): Observable<T> {
    request.versionClient = UtilClient.versionClient
    // Grid RowCellList
    if (request.commandName == 'CommandGridLoad') {
      const gridRequest = structuredClone(request.paramList![0] as GridRequestDto)
      if (gridRequest.grid.rowCellList) {
        gridRequest.grid.rowCellList = undefined
      }
      if (gridRequest.parentGrid?.rowCellList) {
        gridRequest.parentGrid.rowCellList = undefined
      }
      request.paramList![0] = gridRequest
    }
    return of(0).pipe(
      tap(() => {
        // this.notificationService.list.update(() => []) // TODO Can not empty list. There might be multiple sequential requests on same page.
        this.postCountAdd(1)
        this.notificationService.cacheCount.update(() => undefined)
      }),
      // Param withCredentials to send SessionId cookie to server. 
      // Add CORS (not *) https://www.example.com and enable Enable Access-Control-Allow-Credentials on server
      mergeMap(() => {
        const configuration = this.configuration()
        if (configuration.isDevelopment) {
          request.developmentSessionId = localStorage.getItem('developmentSessionId') ?? undefined
          request.developmentCacheId = localStorage.getItem('developmentCacheId') ?? undefined
        }
        return this.httpClient.post<ResponseDto>(configuration.serverUrl, request, { withCredentials: configuration.isDevelopment == false }).pipe(
          tap(value => {
            // DevelopmentSessionId
            if (configuration.isDevelopment) {
              if (value.developmentSessionId) {
                localStorage.setItem('developmentSessionId', value.developmentSessionId)
              }
              if (value.developmentCacheId) {
                localStorage.setItem('developmentCacheId', value.developmentCacheId)
              }
            }
            // NavigateUrl
            if (value.navigateUrl) {
              this.navigate(value.navigateUrl)
            }
            // Notification
            if (value.notificationList) {
              this.notificationService.list.update(list => {
                if (value.notificationList) {
                  value.notificationList = value.notificationList.reverse()
                  list = [...value.notificationList, ...list]
                }
                return list
              })
            }
            this.notificationService.cacheCount.update(() => value.cacheCount)
            // Reload
            if (value.isReload) {
              setTimeout(() => {
                window.location.reload()
              }, 3000);
            }
          }),
          map(value => {
            this.postCountAdd(-1);
            return value.result as T
          }),
          catchError(error => {
            this.postCountAdd(-1);
            // Reload
            if (error.error?.isReload) {
              this.notificationService.add(NotificationEnum.Info, "Info: " + error.error.exceptionText) // Show reload as info not as exception
              setTimeout(() => {
                window.location.reload()
              }, 3000);
            } else {
              // NavigateUrl
              if (error.error?.navigateUrl) {
                this.navigate(error.error.navigateUrl)
              }
              // Notification
              if (error.error?.exceptionText) {
                this.notificationService.add(NotificationEnum.Error, "Exception: " + error.error.exceptionText)
                if (this.isBrowser()) {
                  window.scroll({ top: 0, behavior: 'smooth' }) // Scroll to top when notification has been added.
                }
                throw error
              }
              this.notificationService.add(NotificationEnum.Error, "Error: " + "Network failure!")
            }
            throw error
          })
        )
      })
    )
  }

  async fileDownload(fileName: string, fileUrl: string) {
    var result = await firstValueFrom(this.httpClient.get(fileUrl, { responseType: 'blob' }))
    return result
  }

  async fileUpload(file: File, fileUrl: string) {
    const headers = new HttpHeaders({
      'x-ms-blob-type': 'BlockBlob'
    });
    await firstValueFrom(
      this.httpClient.put(fileUrl, file, { headers }) // CORS Allowed origins http://localhost:4200 Allowed methods PUT, GET Allowed headers content-type,x-ms-*" // content-type esed for *.png files
    );
  }

  commandVersion() {
    return this.post<string>({ commandName: "CommandVersion" })
  }

  commandTree(componentDto?: ComponentDto) {
    return this.post<ComponentDto>({ commandName: "CommandTree", paramList: [componentDto] })
  }

  commandDebug() {
    return this.post<ResponseDto>({ commandName: "CommandDebug" })
  }

  commandStorageDownload(fileName: string) {
    return this.post<string>({ commandName: "CommandStorageDownload", paramList: [fileName] })
  }

  commmandStorageUpload(fileName: string, data: string) {
    return this.post<void>({ commandName: "CommandStorageUpload", paramList: [fileName, data] })
  }

  async commmandUserSignStatus() {
    return await firstValueFrom(this.post<UserStatusDto>({ commandName: "CommandUserSignStatus", paramList: [] }))
  }

  async commmandUserSignIn(userDto: UserDto) {
    return await firstValueFrom(this.post<void>({ commandName: "CommandUserSignIn", paramList: [userDto] }))
  }

  async commmandUserSignUp(userDto: UserDto) {
    return await firstValueFrom(this.post<void>({ commandName: "CommandUserSignUp", paramList: [userDto] }))
  }

  async commmandUserSignOut() {
    return await firstValueFrom(this.post<void>({ commandName: "CommandUserSignOut", paramList: [] }))
  }

  async commmandArticleAdd() {
    return await firstValueFrom(this.post<void>({ commandName: "CommandArticleAdd", paramList: [] }))
  }

  async commandGridLoad2(request: GridRequest2Dto) {
    return await firstValueFrom(this.post<GridResponse2Dto>({ commandName: "CommandGridLoad2", paramList: [request] }))
  }
}