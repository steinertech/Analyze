import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { map, Observable, tap } from "rxjs"
import { DataService } from "./data.service"
import { Router } from "@angular/router"

export class RequestDto {
  public commandName!: string
  public paramList?: any[]
}

export class ResponseDto {
  public result?: any
  public exception?: string
  public navigateUrl?: string
}

export class ComponentDto {
  public list?: ComponentDto[]
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

@Injectable({
  providedIn: 'root',
})
export class ServerApi {
  constructor(private httpClient: HttpClient, private router: Router, public dataService: DataService) {

  }

  post<T>(request: RequestDto ): Observable<T> {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), request).pipe(
      // NavigateUrl
      tap(value => {
        if (value.navigateUrl) {
          this.router.navigateByUrl(value.navigateUrl)
        }
      }),
      map(value => <T>value.result)
    )
  }

  commandVersion() {
    return this.post<string>({ commandName: "CommandVersion" });
  }

  commandTree(componentDto?: ComponentDto) {
    return this.post<ComponentDto>({ commandName: "CommandTree", paramList: [componentDto] });
  }

  commandDebug() {
    return this.post<ResponseDto>({ commandName: "CommandDebug" });
  }

  commandStorageDownload(fileName: string) {
    return this.post<string>({ commandName: "CommandStorageDownload", paramList: [fileName] });
  }

  commmandStorageUpload(fileName: string, data: string) {
    return this.post<void>({ commandName: "CommandStorageUpload", paramList: [fileName, data] });
  }

  commmandUserSignUp(userDto: UserDto) {
    return this.post<void>({ commandName: "CommandUserSignUp", paramList: [userDto] });
  }
}
