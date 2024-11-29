import { Injectable } from '@angular/core';
import { Exception } from 'handlebars';
import { settings } from 'src/app/core/models/settings';
import { MediaFile } from 'src/app/core/services/media-api.service';

@Injectable({
  providedIn: 'root'
})
export class FileUtilitiesService {

  constructor() { }

  public getCoverImageUrl(file: MediaFile, size?: number) {
    switch (file.fileExtension) {
      case '.csv': 
        return '../../../assets/images/csv-icon.png';
      case '.docx': 
        return '../../../assets/images/word-icon.png';
      case '.xlsx': 
        return '../../../assets/images/excel-icon.png';
      case '.pdf': 
        return '../../../assets/images/pdf-icon.png';
      case '.pptx': 
        return '../../../assets/images/pptx-icon.png';
      default:
        return file.permaLink + `?size=${size || ''}`;
    }
  }

  public async getFileTemplate(file: MediaFile) {
    switch (file.fileExtension) {
      case '.csv' :
      case '.docx': 
      case '.xlsx': 
      case '.pdf': 
      case '.pptx': 
        return `<a href="${file.permaLink}" download>${file.name}</a>`;
      default:
        return `<img title="${file.name}" src="${file.permaLink}" alt="${file.name}"></img>`;
    }
  }

  private _toDataURL = (url: string) => fetch(url)
  .then(response => response.blob())
  .then(blob => new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onloadend = () => resolve(reader.result)
    reader.onerror = reject
    reader.readAsDataURL(blob)
  }))

  public copyPathToClipboard(path: string | undefined) {
    return navigator.clipboard.writeText(path ?? '')
      .catch((err) => {
        throw err;
    });
  }

  public openFileInNewTab(file: MediaFile) {
      window.open(file?.permaLink, '_blank');
  }

  public async downloadFile(file: MediaFile | undefined) {
    var url = file?.permaLink ?? '';
    let blob = await fetch(url).then(r => r.blob());
    const blobUrl = window.URL.createObjectURL(new Blob([blob]));
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = blobUrl;
    a.download = file?.name ?? 'download';
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(blobUrl);
  }
}
