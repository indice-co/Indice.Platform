import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UploadFileWidgetService {
  public files: any;

  constructor() { }

  reset() {
    this.files = undefined;
  }
}
