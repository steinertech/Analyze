import { Component } from '@angular/core';
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";
import { PageStorageUpload } from "../page-storage-upload/page-storage-upload";

@Component({
  selector: 'app-page-storage',
  imports: [PageNav, PageNotification, PageStorageUpload],
  templateUrl: './page-storage.html',
  styleUrl: './page-storage.css'
})
export class PageStorage {

}
