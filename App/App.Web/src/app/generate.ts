import { HttpClient } from "@angular/common/http"
import { inject, Injectable, signal } from "@angular/core"
import { catchError, firstValueFrom, map, mergeMap, Observable, of, tap } from "rxjs"
import { Router } from "@angular/router"
import { NotificationDto, NotificationEnum, NotificationService } from "./notification.service"
import { UtilClient } from "./util-client"

export class RequestDto {
  public commandName!: string
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  public paramList?: any[]
  public developmentSessionId?: string
  public developmentCacheId?: string
  public versionClient?: string
}

export class ResponseDto {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  public result?: any
  public exceptionText?: string
  public navigateUrl?: string
  public notificationList?: NotificationDto[]
  public developmentSessionId?: string
  public developmentCacheId?: string
  public isReload?: boolean
  public cacheCount?: number
}

export class ComponentDto {
  public list?: ComponentDto[]
}

export enum NatificationEnum {
  None = 0,
  Info = 1,
  Success = 2,
  Warning = 3,
  Error = 4,
}

export class ComponentButtonDto extends ComponentDto {
  public text?: string
  public isClick?: boolean
}

export class ComponentTextDto extends ComponentDto {
  public text?: string
}

export class UserDto {
  public email?: string
  public password?: string
}

export class ProductDto {
  public text?: string
  public storageFileName?: string
  public price?: number
}

export class GridRequestDto {
  public grid!: GridDto
  public parentGrid?: GridDto
  public cell?: GridCellDto
  public control?: GridControlDto
  public parentCell?: GridCellDto
  public parentControl?: GridControlDto
}

export class GridResponseDto {
  public grid!: GridDto
  public parentGrid?: GridDto
}

export class GridRequest2EntryDto {
  public grid?: GridDto
  public cell?: GridCellDto
  public control?: GridControlDto
}

export class GridRequest2Dto {
  public list!: GridRequest2EntryDto[]
}

export class GridResponse2Dto {
  public list!: GridDto[]
}

export class GridConfigDto {
  public gridName!: string
  public dataTableName!: string
  public isAllowUpdate?: boolean
  public isAllowInsert?: boolean
  public isAllowDelete?: boolean
  public grid?: GridDto
}

export class GridConfigFieldDto {
  public fieldName!: string
  public text?: string
  public isDropdown?: boolean
}

export class GridCellDto {
  public cellEnum?: GridCellEnum
  public dataRowIndex?: number
  public fieldName?: string
  public text?: string
  public textPlaceholder?: string
  public textModified?: string
  public dropdownList?: (string | null)[]
  public controlList?: GridControlDto[]
  public iconLeft?: GridCellIconDto
  public iconRight?: GridCellIconDto
  public colSpan?: number
  public rowSpan?: number
}

export class GridCellIconDto {
  public className?: string
  public tooltip?: string
}

export class GridControlDto {
  public controlEnum?: GridControlEnum
  public text?: string
  public textModified?: string
  public name?: string
}

export enum GridCellEnum {
  None = 0,
  Field = 1,
  Header = 2,
  HeaderEmpty = 3,
  FieldDropdown = 5,
  Filter = 10,
  FilterEmpty = 14,
  FieldCheckbox = 11,
  FieldAutocomplete = 12,
  CheckboxSelectMulti = 13,
  Control = 16,
}

export enum GridControlEnum {
  None = 0,
  ButtonReload = 3,
  ButtonSave = 4,
  ButtonLookupCancel = 8,
  ButtonLookupOk = 7,
  ButtonLookupSort = 9,
  ButtonColumn = 15,
  ButtonCustom = 16,
  CheckboxSelectMultiAll = 14,
  LabelCustom = 17,
  FieldCustom = 18,
  ButtonModal = 19,
  Pagination = 20,
}

export class GridDto {
  public gridName!: string
  // dataRowList?: any[]
  // gridConfig?: GridConfigDto
  public rowCellList?: GridCellDto[][]
  public parentGridName?: string
  public state?: GridStateDto
  // public editRowIndex?: number
  // public editFieldName?: string
  // public selectRowIndex?: number
  // public selectFieldName?: string
}

export class GridStateDto {
  public sort?: GridStateSortDto
  public filterList?: Record<string, string>
  public filterMultiList?: Record<string, GridStateFilterMultiDto>
  public isMouseEnterList?: (boolean | null)[]
  public isSelectList?: (boolean | null)[]
  public isSelectMultiList?: (boolean | null)[]
  public isSelectMultiAll?: (boolean | null)
  public isSelectMultiIndeterminate?: (boolean | null)
  public columnList?: GridStateColumnDto[]
  public columnWidthList?: (number | null)[]
  public rowKeyList?: (string | null)[]
  public rowKeyMasterList?: Record<string, string | null>
  public pagination?: GridPaginationDto
  public fieldSaveList?: FieldSaveDto[]
}

export class FieldSaveDto {
  public dataRowIndex?: number
  public fieldName?: string
  public text?: string
  public textModified?: string
}

export class GridPaginationDto {
  public pageIndex?: number
  public pageCount?: number
  public pageSize?: number
  public pageIndexDeltaClick?: number
}

export class GridStateColumnDto {
  public fieldName!: string
  public orderBy!: number
}

export class GridStateSortDto {
  public fieldName!: string
  public isDesc!: boolean
}

export class GridStateFilterDto {
  public fieldName!: string
  public text!: string
}

export class GridStateFilterMultiDto {
  public textList!: string[]
}

@Injectable({
  providedIn: 'root',
})
export class ServerApi {
  private httpClient = inject(HttpClient)
  private router = inject(Router)
  private notificationService = inject(NotificationService)

  public isWindow() {
    return typeof window !== "undefined"
  }

  private isLocalhost() {
    let result = false
    if (this.isWindow()) {
      const hostname = window.location.hostname
      result =
        hostname == "localhost" || // Running in VS code
        hostname == '127.0.0.1' // Running with http-server (ng build --localize)
    }
    return result
  }

  private isLocalhostGitHubCodeSpace() {
    let result = false
    if (this.isWindow()) {
      const hostname = window.location.hostname
      result =
        hostname.endsWith('github.dev') // Running on GitHub CodeSpace
    }
    return result
  }

  /** Returns configuration based on client domain. */
  private configuration() {
    const partList = window.location.hostname.split('.')
    partList[0] = 'api' // www.example.com to api.example.com
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
    return await firstValueFrom(this.post<UserDto>({ commandName: "CommandUserSignStatus", paramList: [] }))
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