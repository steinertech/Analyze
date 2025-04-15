import { HttpClient } from "@angular/common/http"
import { Injectable } from "@angular/core"
import { map } from "rxjs"

export interface RequestDto {
  commandName: string
}

export interface ResponseDto {
  commandName: string
  exceptionText: string
}

export interface ResponseVersionDto extends ResponseDto {
  result: string
}

@Injectable({
  providedIn: 'root',
})
export class AppServerCommand {
  constructor(private httpClient: HttpClient) {

  }

  CommandVersion() {
    return this.httpClient.post<ResponseVersionDto>("http://localhost:7138/api/data", <RequestDto>{ commandName: "CommandVersion" }).pipe(map(response => response.result));
  }
}
