import { Component, inject, signal } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { ComponentDto, ServerApi } from '../generate';
import { DataService } from '../data.service';
import { UtilClient } from '../util-client';
import { CommonModule } from '@angular/common';
import { PageNotification } from "../page-notification/page-notification";

@Component({
  selector: 'app-page-about',
  imports: [
    PageNav,
    CommonModule,
    PageNotification
  ],
  templateUrl: './page-about.html',
  styleUrl: './page-about.css'
})
export class PageAbout {
  private serverApi = inject(ServerApi)
  public dataService = inject(DataService)

  text = $localize`:@@debugKeyTs:Hello Ts (Native)`

  versionClient: string = UtilClient.versionClient

  readonly versionServer = signal<string | undefined>(undefined)

  readonly componentDto = signal<ComponentDto | undefined>(undefined)

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly debugDto = signal<any>(undefined)

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly storageContent = signal<any>(undefined)

  click() {
    this.serverApi.commandVersion().subscribe(result => this.versionServer.set(result))
    this.serverApi.commandTree(this.componentDto()).subscribe(result => this.componentDto.set(result))
    this.serverApi.commandDebug().subscribe(result => this.debugDto.set(result))
    this.serverApi.commandStorageDownload('a.txt').subscribe(result => this.storageContent.set(result))
  }
}
