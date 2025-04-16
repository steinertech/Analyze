import { Component } from '@angular/core';
import { PageNavComponent } from '../page-nav/page-nav.component';
import { ComponentDto, ServerApi } from '../generate';
import { CommonModule } from '@angular/common';
import { DataService } from '../data.service';
import { PageStorageUploadComponent } from '../page-storage-upload/page-storage-upload.component';

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
  constructor(public serverApi: ServerApi, public dataService: DataService) {
  }

  componentDto?: ComponentDto

  debugDto?: any

  version?: string

  storageContent?: any

  click() {
    this.serverApi.CommandVersion().subscribe(result => this.version = result)
    this.serverApi.CommandTree(this.componentDto).subscribe(result => this.componentDto = result)
    this.serverApi.CommandDebug().subscribe(result => this.debugDto = result)
    this.serverApi.CommandStorageDownload('a.txt').subscribe(result => this.storageContent = result)
  }
}
