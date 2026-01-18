import { Component, ViewChild } from '@angular/core';
import { PageGrid } from "../page-grid/page-grid";
import { PageNav } from "../page-nav/page-nav";
import { PageNotification } from "../page-notification/page-notification";

@Component({
  selector: 'app-page-schema',
  imports: [PageGrid, PageNav, PageNotification],
  templateUrl: './page-schema.html',
  styleUrl: './page-schema.css'
})
export class PageSchema {
  @ViewChild('gridSchemaTable') gridSchemaTable!: PageGrid;
  @ViewChild('gridSchemaField') gridSchemaField!: PageGrid;

  async ngAfterViewInit() {
    await this.gridSchemaTable.load2('SchemaTable')
    await this.gridSchemaField.load2('SchemaField')
  }
}
