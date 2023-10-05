import { Injectable } from '@angular/core';
import { settings } from 'src/app/core/models/settings';
import { MediaFile } from 'src/app/core/services/media-api.service';

@Injectable({
  providedIn: 'root'
})
export class FileUtilitiesService {

  constructor() { }

  public getCoverImageUrl(file: MediaFile) {
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
        return file.permaLink;
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

  public copyPermaLinkToClipboard(file: MediaFile) {
    const el = document.createElement("textarea");
    el.value = file.permaLink ?? '';
    el.setAttribute("readonly", "");
    el.style.position = "absolute";
    el.style.left = "-9999px";
    document.body.appendChild(el);
    const selected =
      document?.getSelection()?.rangeCount ?? 0 > 0
        ? document?.getSelection()?.getRangeAt(0)
        : false;
    el.select();
    document.execCommand("copy");
    document.body.removeChild(el);
    if (selected) {
      document?.getSelection()?.removeAllRanges();
      document?.getSelection()?.addRange(selected);
    }
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
