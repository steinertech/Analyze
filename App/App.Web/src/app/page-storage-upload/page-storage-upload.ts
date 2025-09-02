import { Component, inject, NgZone } from '@angular/core';
import { ServerApi } from '../generate';

@Component({
  selector: 'app-page-storage-upload',
  imports: [],
  templateUrl: './page-storage-upload.html',
  styleUrl: './page-storage-upload.css'
})
export class PageStorageUpload {
  private serverApi = inject(ServerApi)
  private ngZone = inject(NgZone)

  onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    const files = target.files;

    if (files && files.length > 0) {
      for (const file of files) {
        this.storageUpload(file);
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
      for (const file of event.dataTransfer.files) {
        this.storageUpload(file);
      }
    }
  }

  storageUpload(file: File) {
    console.log("Selected file:", file.name);
    const reader = new FileReader()
    reader.readAsDataURL(file)
    reader.onload = () => {
      // eslint-disable-next-line @typescript-eslint/no-non-null-asserted-optional-chain
      const base64String = reader.result?.toString().split(",")[1]!
      this.ngZone.run(() =>{
        this.serverApi.commmandStorageUpload(file.name, base64String).subscribe();
      })
    }
  }
}
