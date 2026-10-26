import { Component, ElementRef, OnDestroy, inject, signal, viewChild } from '@angular/core';
import { LucideImageUp, LucideTrash, LucideZoomIn } from '@lucide/angular';
import { IdentityApiService } from 'src/app/core/services/identity-api.service';
import { ImgUserPictureDirective } from 'src/app/ui/avatar/user-picture.directive';
import { DIALOG_DATA, DialogRef } from 'src/app/ui/dialog/dialog-ref';
import { ToastService } from 'src/app/ui/toast/toast.service';

export interface ProfilePictureDialogData {
  userId: string;
  displayName: string;
  editable: boolean;
  version: number;
}

type Orientation = 'landscape' | 'portrait' | 'square';

/**
 * Shows a user's profile picture and lets administrators upload a new one (dragged and zoomed inside a round viewport,
 * the server crops it with the same scale / translate values) or remove it. Closes with `true` when the picture changed.
 */
@Component({
  selector: 'app-profile-picture-dialog',
  imports: [ImgUserPictureDirective, LucideImageUp, LucideTrash, LucideZoomIn],
  template: `
    <h3 class="text-lg font-semibold">Profile picture</h3>
    <p class="mt-1 text-sm text-base-content/70">{{ data.displayName }}</p>

    <div class="mt-5 flex flex-col items-center gap-4">
      <div class="relative overflow-hidden rounded-full bg-base-200 ring-4 ring-base-300 select-none"
        [style.width.px]="viewPort" [style.height.px]="viewPort"
        [class.cursor-grab]="interactive()" [class.cursor-grabbing]="dragging()"
        (wheel)="onWheel($event)" (mousedown)="onDragStart($event)" (mousemove)="onDrag($event)" (mouseup)="onDragEnd()" (mouseleave)="onDragEnd()"
        (touchstart)="onDragStart($event)" (touchmove)="onDrag($event)" (touchend)="onDragEnd()">
        <img #preview class="size-full object-contain" [class.hidden]="!fileUrl()" [src]="fileUrl() ?? ''" alt="" draggable="false" (load)="onPreviewLoaded()" />
        @if (!fileUrl()) {
          <img class="size-full object-cover" [userPicture]="data.userId" [displayName]="data.displayName" [size]="256" [version]="data.version"
            alt="Current profile picture" />
        }
        @if (hint()) {
          <span class="pointer-events-none absolute inset-0 grid place-items-center bg-neutral/55 text-sm font-medium text-neutral-content">
            Drag to reposition
          </span>
        }
      </div>

      @if (interactive()) {
        <label class="flex w-full max-w-xs items-center gap-3 text-sm text-base-content/70">
          <svg lucideZoomIn class="size-4 shrink-0"></svg>
          <input type="range" class="range range-xs range-primary" [min]="minScale" [max]="maxScale" [step]="step" [value]="scale"
            [disabled]="maxScale <= minScale" (input)="onScale(+$any($event.target).value)" aria-label="Zoom" />
        </label>
      }
    </div>

    <input #file type="file" accept="image/*" class="hidden" (change)="onFileSelected($event)" />
    <div class="modal-action flex-wrap">
      @if (data.editable && !fileUrl()) {
        <button type="button" class="btn btn-ghost text-error" [disabled]="busy()" (click)="remove()">
          <svg lucideTrash class="size-4"></svg>Remove
        </button>
      }
      <span class="flex-1"></span>
      <button type="button" class="btn btn-ghost" (click)="ref.close(false)">{{ data.editable ? 'Cancel' : 'Close' }}</button>
      @if (data.editable) {
        <button type="button" class="btn" [class.btn-primary]="!fileUrl()" (click)="fileInput().nativeElement.click()">
          <svg lucideImageUp class="size-4"></svg>{{ fileUrl() ? 'Choose another' : 'Upload picture' }}
        </button>
        @if (fileUrl()) {
          <button type="button" class="btn btn-primary" [disabled]="busy() || !interactive()" (click)="save()">
            @if (busy()) { <span class="loading loading-spinner loading-sm"></span> }
            Save
          </button>
        }
      }
    </div>
  `
})
export class ProfilePictureDialogComponent implements OnDestroy {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _preview = viewChild.required<ElementRef<HTMLImageElement>>('preview');
  protected readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('file');
  protected readonly data = inject<ProfilePictureDialogData>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);

  protected readonly viewPort = 320;
  protected readonly step = 0.05;
  protected readonly fileUrl = signal<string | null>(null);
  protected readonly interactive = signal(false);
  protected readonly hint = signal(false);
  protected readonly dragging = signal(false);
  protected readonly busy = signal(false);

  private _file: File | null = null;
  private _orientation: Orientation = 'square';
  protected scale = 1;
  protected minScale = 1;
  protected maxScale = 1;
  private _prevScale = 1;
  private _translateX = 0;
  private _translateY = 0;
  private _drag: { x: number; y: number } | null = null;
  private _initialLeftBound = 0;
  private _initialTopBound = 0;
  private _leftBound = 0;
  private _rightBound = 0;
  private _topBound = 0;
  private _bottomBound = 0;

  public ngOnDestroy(): void {
    this.revoke();
  }

  protected onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }
    this.reset();
    this._file = file;
    this.fileUrl.set(URL.createObjectURL(file));
  }

  protected onPreviewLoaded(): void {
    if (!this.fileUrl()) {
      return;
    }
    this.setup();
    this.interactive.set(true);
    this.hint.set(true);
  }

  protected onDragStart(event: MouseEvent | TouchEvent): void {
    if (!this.interactive()) {
      return;
    }
    event.preventDefault();
    this.hint.set(false);
    const point = this.point(event);
    this._drag = { x: point.x - this._translateX, y: point.y - this._translateY };
    this.dragging.set(true);
  }

  protected onDrag(event: MouseEvent | TouchEvent): void {
    if (!this.interactive() || !this._drag) {
      return;
    }
    event.preventDefault();
    const point = this.point(event);
    this._translateX = Math.min(Math.max(point.x - this._drag.x, this._rightBound), this._leftBound);
    this._translateY = Math.min(Math.max(point.y - this._drag.y, this._bottomBound), this._topBound);
    this.transform();
  }

  protected onDragEnd(): void {
    this._drag = null;
    this.dragging.set(false);
  }

  protected onWheel(event: WheelEvent): void {
    if (!this.interactive()) {
      return;
    }
    event.preventDefault();
    this.onScale(this.scale + (event.deltaY < 0 ? this.step : -this.step));
  }

  protected onScale(value: number): void {
    const scale = Math.max(this.minScale, Math.min(value, this.maxScale));
    if (scale < this._prevScale) {
      this.scaleDownWithinBounds(scale);
    }
    this.scale = scale;
    this._prevScale = scale;
    this.calculateBounds();
    this.transform();
  }

  protected save(): void {
    if (!this._file) {
      return;
    }
    this.busy.set(true);
    this._api.saveUserPicture(this.data.userId, { fileName: this._file.name, data: this._file }, this.scale, this._translateX, this._translateY, this.viewPort)
      .subscribe({
        next: () => {
          this._toast.success('Profile picture updated');
          this.ref.close(true);
        },
        error: () => this.busy.set(false)
      });
  }

  protected remove(): void {
    this.busy.set(true);
    this._api.clearUserPicture(this.data.userId).subscribe({
      next: () => {
        this._toast.success('Profile picture removed');
        this.ref.close(true);
      },
      error: () => this.busy.set(false)
    });
  }

  private point(event: MouseEvent | TouchEvent): { x: number; y: number } {
    return event instanceof MouseEvent ? { x: event.clientX, y: event.clientY } : { x: event.touches[0].clientX, y: event.touches[0].clientY };
  }

  /** Fits the picture into the viewport; the math matches what the server expects from `scale` / `translateX` / `translateY`. */
  private setup(): void {
    const image = this._preview().nativeElement;
    const { naturalWidth, naturalHeight } = image;
    const scaledWidth = image.width * (this.viewPort / image.height);
    if (naturalHeight > naturalWidth) {
      this._orientation = 'portrait';
      this.scale = (naturalHeight / naturalWidth) * (this.viewPort / scaledWidth);
      this.maxScale = naturalWidth / this.viewPort;
      this._initialTopBound = Math.round((image.height * this.scale - this.viewPort) / 2 / this.scale);
      this._initialLeftBound = 0;
    } else if (naturalWidth > naturalHeight) {
      this._orientation = 'landscape';
      this.scale = (naturalWidth / naturalHeight) * this.scale;
      this.maxScale = naturalHeight / this.viewPort;
      this._translateX = -scaledWidth / 2 + this.viewPort / 2;
      this._translateY = 0;
      this._initialLeftBound = Math.round((image.height * this.scale - this.viewPort) / 2 / this.scale);
    } else {
      this._orientation = 'square';
      this.scale = (naturalWidth / naturalHeight) * this.scale;
      this.maxScale = naturalHeight / this.viewPort;
    }
    this.maxScale = Math.max(this.maxScale, this.scale);
    this.minScale = this.scale;
    this._prevScale = this.scale;
    this.calculateBounds();
    this.transform();
  }

  private calculateBounds(): void {
    const image = this._preview().nativeElement;
    const overflow = (image.height * this.scale - this.viewPort) / 2;
    if (this._orientation === 'portrait') {
      this._topBound = Math.round(overflow / this.scale - this._initialLeftBound);
      this._bottomBound = -this._topBound;
      this._leftBound = this._topBound - this._initialTopBound;
      this._rightBound = -this._leftBound;
    } else {
      this._leftBound = Math.round(overflow / this.scale);
      this._rightBound = -this._leftBound;
      this._topBound = Math.round(this._leftBound - this._initialLeftBound);
      this._bottomBound = -this._topBound;
    }
  }

  private scaleDownWithinBounds(scale: number): void {
    this._translateX = Math.max(this._rightBound, Math.min(this._translateX, this._leftBound));
    this._translateY = Math.max(this._bottomBound, Math.min(this._translateY, this._topBound));
    this._translateX -= this._translateX / scale;
    this._translateY -= this._translateY / scale;
  }

  private transform(): void {
    this._preview().nativeElement.style.transform = `scale(${this.scale}) translate(${this._translateX}px, ${this._translateY}px)`;
  }

  private reset(): void {
    this.revoke();
    this.interactive.set(false);
    this.hint.set(false);
    this._orientation = 'square';
    this.scale = this.minScale = this.maxScale = this._prevScale = 1;
    this._translateX = this._translateY = 0;
    this._initialLeftBound = this._initialTopBound = 0;
    this._leftBound = this._rightBound = this._topBound = this._bottomBound = 0;
    this._preview().nativeElement.style.transform = '';
  }

  private revoke(): void {
    const url = this.fileUrl();
    if (url) {
      URL.revokeObjectURL(url);
      this.fileUrl.set(null);
    }
  }
}
