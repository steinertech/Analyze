import { Component, signal, Signal } from '@angular/core';
import { PageNav } from '../page-nav/page-nav';
import { ComponentDto, ServerApi } from '../generate';
import { DataService } from '../data.service';
import { UtilClient } from '../util-client';
import { PageStorageUpload } from '../page-storage-upload/page-storage-upload';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-page-about',
  imports: [
    PageNav,
    PageStorageUpload,
    CommonModule, // Async pipe    
  ],
  templateUrl: './page-about.html',
  styleUrl: './page-about.css'
})
export class PageAbout {
  text = $localize`:@@debugKeyTs:Hello Ts (Native)`

  constructor(private serverApi: ServerApi, public dataService: DataService) {
  }

  versionClient: string = UtilClient.versionClient

  readonly versionServer = signal<string | undefined>(undefined)

  readonly componentDto = signal<ComponentDto | undefined>(undefined)

  readonly debugDto = signal<any>(undefined)

  readonly storageContent = signal<any>(undefined)

  click() {
    this.serverApi.commandVersion().subscribe(result => this.versionServer.set(result))
    this.serverApi.commandTree(this.componentDto()).subscribe(result => this.componentDto.set(result))
    this.serverApi.commandDebug().subscribe(result => this.debugDto.set(result))
    this.serverApi.commandStorageDownload('a.txt').subscribe(result => this.storageContent.set(result))
  }  
}
