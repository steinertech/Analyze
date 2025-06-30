import { Component } from '@angular/core';
import { DataService } from '../data.service';

@Component({
  selector: 'app-page-notification',
  imports: [],
  templateUrl: './page-notification.html',
  styleUrl: './page-notification.css'
})
export class PageNotification {
  constructor(public dataService: DataService) {
  }
}
