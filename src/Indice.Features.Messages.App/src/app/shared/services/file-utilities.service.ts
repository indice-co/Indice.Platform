import { Injectable } from '@angular/core';
import {
  LucideFile, LucideFileArchive, LucideFileAudio, LucideFileCode, LucideFileImage, LucideFileSpreadsheet, LucideFileText,
  LucideFileVideo, LucideIcon, LucidePresentation
} from '@lucide/angular';
import { settings } from 'src/app/core/models/settings';
import { MediaFile } from 'src/app/core/services/media-api.service';

const IMAGE_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.gif', '.webp', '.bmp', '.svg', '.ico', '.avif'];
const DOCUMENT_EXTENSIONS = ['.csv', '.docx', '.xlsx', '.pdf', '.pptx'];

const ICONS: Record<string, LucideIcon> = {
  '.pdf': LucideFileText, '.doc': LucideFileText, '.docx': LucideFileText, '.txt': LucideFileText, '.rtf': LucideFileText, '.odt': LucideFileText,
  '.csv': LucideFileSpreadsheet, '.xls': LucideFileSpreadsheet, '.xlsx': LucideFileSpreadsheet, '.ods': LucideFileSpreadsheet,
  '.ppt': LucidePresentation, '.pptx': LucidePresentation, '.odp': LucidePresentation,
  '.zip': LucideFileArchive, '.rar': LucideFileArchive, '.7z': LucideFileArchive, '.gz': LucideFileArchive, '.tar': LucideFileArchive,
  '.mp4': LucideFileVideo, '.mov': LucideFileVideo, '.webm': LucideFileVideo, '.avi': LucideFileVideo,
  '.mp3': LucideFileAudio, '.wav': LucideFileAudio, '.ogg': LucideFileAudio,
  '.html': LucideFileCode, '.htm': LucideFileCode, '.json': LucideFileCode, '.xml': LucideFileCode, '.css': LucideFileCode, '.js': LucideFileCode
};

/** Helpers for presenting, copying and downloading media library files. */
@Injectable({ providedIn: 'root' })
export class FileUtilitiesService {
  /** Whether the file can be previewed as an image. */
  public isImage(file: Pick<MediaFile, 'isImage' | 'fileExtension'> | undefined): boolean {
    return !!file && (file.isImage === true || IMAGE_EXTENSIONS.includes(this._extension(file.fileExtension)));
  }

  /** Lucide icon representing the file type. */
  public getFileIcon(file: Pick<MediaFile, 'isImage' | 'fileExtension'> | undefined): LucideIcon {
    if (this.isImage(file)) {
      return LucideFileImage;
    }
    return ICONS[this._extension(file?.fileExtension)] ?? LucideFile;
  }

  /**
   * Thumbnail / preview URL of an image file (`size` asks the server for a resized image), or `undefined` for files that
   * are not images and should be rendered with {@link getFileIcon} instead.
   */
  public getCoverImageUrl(file: MediaFile, size?: number): string | undefined {
    if (!this.isImage(file)) {
      return undefined;
    }
    return size ? `${settings.api_url}/media-root${file.path}?size=${size}` : file.permaLink;
  }

  /** HTML snippet that embeds the file: an `<img>` for images, a download link otherwise. */
  public async getFileTemplate(file: MediaFile): Promise<string> {
    return DOCUMENT_EXTENSIONS.includes(this._extension(file.fileExtension))
      ? `<a href="${file.permaLink}" download>${file.name}</a>`
      : `<img title="${file.name}" src="${file.permaLink}" alt="${file.name}"></img>`;
  }

  public copyPathToClipboard(path: string | undefined): Promise<void> {
    return navigator.clipboard.writeText(path ?? '');
  }

  public openFileInNewTab(file: MediaFile | undefined): void {
    if (file?.permaLink) {
      window.open(file.permaLink, '_blank', 'noopener');
    }
  }

  public async downloadFile(file: MediaFile | undefined): Promise<void> {
    if (!file?.permaLink) {
      return;
    }
    const blob = await fetch(file.permaLink).then(response => response.blob());
    const blobUrl = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.style.display = 'none';
    anchor.href = blobUrl;
    anchor.download = file.name || 'download';
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(blobUrl);
  }

  /** Human readable byte size, e.g. `1.4 MB`. */
  public formatSize(bytes: number | undefined): string {
    if (bytes === undefined || bytes === null || isNaN(bytes)) {
      return '';
    }
    const units = ['B', 'KB', 'MB', 'GB'];
    let value = bytes;
    let unit = 0;
    while (value >= 1024 && unit < units.length - 1) {
      value /= 1024;
      unit++;
    }
    return `${unit === 0 ? value : Math.round(value * 10) / 10} ${units[unit]}`;
  }

  private _extension(extension: string | undefined): string {
    if (!extension) {
      return '';
    }
    const lower = extension.toLowerCase();
    return lower.startsWith('.') ? lower : `.${lower}`;
  }
}
