import { Component, NgZone } from '@angular/core';
import { ServerApi } from '../generate';

@Component({
  selector: 'app-page-storage-upload',
  imports: [],
  templateUrl: './page-storage-upload.component.html',
  styleUrl: './page-storage-upload.component.css'
})
export class PageStorageUploadComponent {
  constructor(private serverApi: ServerApi, private ngZone: NgZone) { 
  }

  onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const files = target.files;

    if (files && files.length > 0) {
      for (let i = 0; i < files.length; i++) {
        this.storageUpload(files[i]);
      }
    }
    (event.target as HTMLInputElement).value = "" // Necessary to select and upload same file multiple times.
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();

    if (event.dataTransfer?.files.length) {
      for (let i = 0; i < event.dataTransfer.files.length; i++) {
        this.storageUpload(event.dataTransfer.files[i]);
      }
    }
  }

  storageUpload(file: File) {
    console.log("Selected file:", file.name);
    const reader = new FileReader()
    reader.readAsDataURL(file)
    reader.onload = () => {
      const base64String = reader.result?.toString().split(",")[1]!
      this.ngZone.run(() =>{
        this.serverApi.commmandStorageUpload(file.name, base64String).subscribe();
      })
    }
  }
}
