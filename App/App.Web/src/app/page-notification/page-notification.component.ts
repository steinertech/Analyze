import { Component } from '@angular/core';
import { DataService } from '../data.service';

@Component({
  selector: 'app-page-notification',
  imports: [],
  templateUrl: './page-notification.component.html',
  styleUrl: './page-notification.component.css'
})
export class PageNotificationComponent {
  constructor(public dataService: DataService) {
    }
}
