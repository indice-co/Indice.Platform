import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class FileUploadService {
  // a dictionary (key -> the field's data path, eg homeAddress.attachmentId, value -> the actual file) that holds the files to be uploaded to server
  public files: any;

  constructor() { }

  /**
   * Empties the files.
   */
  reset() {
    this.files = undefined;
  }
}
