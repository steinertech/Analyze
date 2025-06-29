import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { ComponentDto, ServerApi } from '../generate';
import { CommonModule } from '@angular/common';
import { DataService } from '../data.service';
import { PageStorageUploadComponent } from '../page-storage-upload/page-storage-upload.component';
import { UtilClient } from '../util-client';

@Component({
  selector: 'app-page-about',
  imports: [
    PageNavComponent,
    PageStorageUploadComponent,
    CommonModule, // Async pipe
  ],
  templateUrl: './page-about.component.html',
  styleUrl: './page-about.component.css'
})
export class PageAboutComponent {
  constructor(private serverApi: ServerApi, public dataService: DataService) {
  }

  versionClient: string = UtilClient.versionClient

  versionServer?: string

  componentDto?: ComponentDto

  debugDto?: any

  storageContent?: any

  click() {
    this.serverApi.commandVersion().subscribe(result => this.versionServer = result)
    this.serverApi.commandTree(this.componentDto).subscribe(result => this.componentDto = result)
    this.serverApi.commandDebug().subscribe(result => this.debugDto = result)
    this.serverApi.commandStorageDownload('a.txt').subscribe(result => this.storageContent = result)
  }
}
