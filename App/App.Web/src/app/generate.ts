import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { map } from "rxjs"
import { DataService } from "./data.service"

export class RequestDto {
  public commandName!: string
  public paramList?: any[]
}

export class ResponseDto {
  public result?: any
  public exception?: string
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


@Injectable({
  providedIn: 'root',
})
export class ServerApi {
  constructor(private httpClient: HttpClient, public dataService: DataService) {

  }

  CommandVersion() {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), <RequestDto>{ commandName: "CommandVersion" }).pipe(map(response => <string>response.result));
  }

  CommandTree(componentDto?: ComponentDto) {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), <RequestDto>{ commandName: "CommandTree", paramList: [componentDto] }).pipe(map(response => <ComponentDto>response.result));
  }

  CommandDebug() {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), <RequestDto>{ commandName: "CommandDebug" }).pipe(map(response => response.result));
  }

  CommandStorageDownload(fileName: string) {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), <RequestDto>{ commandName: "CommandStorageDownload", paramList: [fileName] }).pipe(map(response => <string>response.result));
  }

  CommmandStorageUpload(fileName: string, data: string) {
    return this.httpClient.post<ResponseDto>(this.dataService.serverUrl(), <RequestDto>{ commandName: "CommandStorageUpload", paramList: [fileName, data] }).pipe(map(response => <string>response.result));
  }
}
