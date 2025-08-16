import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { catchError, firstValueFrom, map, Observable, tap } from "rxjs"
import { Router } from "@angular/router"
import { DataService, NotificationDto, NotificationEnum } from "./data.service"

export class RequestDto {
  public commandName!: string
  public paramList?: any[]
}

export class ResponseDto {
  public result?: any
  public exceptionText?: string
  public navigateUrl?: string
  public notificationList?: NotificationDto[]
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

export class GridSaveDto {
  public grid!: GridDto
  public parentGrid?: GridDto
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
  public isDropDown?: boolean
}

export class GridCellDto {
  public cellEnum?: GridCellEnum
  public dataRowIndex?: number
  public fieldName?: string
  public text?: string
  public textModified?: string
  public dropDownList?: string[]
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
  FieldDropdown = 5,
  Filter = 10,
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
  public filterList?: GridStateFilterDto[]
  public filterMultiList?: GridStateFilterMultiDto[]
  public isMouseEnterList?: (boolean | null)[]
  public isSelectList?: (boolean | null)[]
  public isSelectMultiList?: (boolean | null)[]
  public columnList?: GridStateColumnDto[]
  public columnWidthList?: (number | null)[]
  public buttonCustomClick?: GridStateButtonCustomClickDto
  public rowKeyList?: (string | null)[]
  public rowKeyMasterList?: Record<string, string | null>
  public pagination?: GridPaginationDto
}

export class GridPaginationDto {
  public pageIndex?: number
  public pageCount?: number
  public pageSize?: number
  public pageIndexClick?: number
}

export class GridStateButtonCustomClickDto {
  public name?: string
  public dataRowIndex?: number
  public fieldName?: string
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
  public fieldName!: string
  public textList!: string[]
}

@Injectable({
  providedIn: 'root',
})
export class ServerApi {
  constructor(private httpClient: HttpClient, private router: Router, public dataService: DataService) {

  }

  post<T>(request: RequestDto): Observable<T> {
    // Param withCredentials to send SessionId cookie to server. 
    // Add CORS (not *) https://www.example.com and enable Enable Access-Control-Allow-Credentials on server
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), request, { withCredentials: true }).pipe(
      tap(value => {
        // NavigateUrl
        if (value.navigateUrl) {
          this.router.navigateByUrl(value.navigateUrl)
        }
        // Notification
        if (value.notificationList) {
          this.dataService.notificationList.update(list => {
            if (value.notificationList) {
              value.notificationList = value.notificationList.reverse()
              list = [...value.notificationList, ...list]
            }
            return list
          })
        }
      }),
      map(value => <T>value.result),
      catchError(error => {
        if (error.error?.exceptionText) {
          this.dataService.notificationAdd(NotificationEnum.Error, "Exception: " + error.error?.exceptionText)
          throw error
        }
        this.dataService.notificationAdd(NotificationEnum.Error, "Error: " + "Network failure!")
        throw error
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

  commandGridLoad(grid: GridDto, parentCell?: GridCellDto, parentControl?: GridControlDto, parentGrid?: GridDto) {
    return this.post<GridDto>({ commandName: "CommandGridLoad", paramList: [grid, parentCell, parentControl, parentGrid] })
  }

  commandGridSave(grid: GridDto, parentCell?: GridCellDto, parentControl?: GridControlDto, parentGrid?: GridDto) {
    return this.post<GridSaveDto>({ commandName: "CommandGridSave", paramList: [grid, parentCell, parentControl, parentGrid] })
  }

  commandGridSelectDropDown(gridName: string, fieldName: string) {
    return this.post<string[]>({ commandName: "CommandGridSelectConfig", paramList: [gridName, fieldName] })
  }
}