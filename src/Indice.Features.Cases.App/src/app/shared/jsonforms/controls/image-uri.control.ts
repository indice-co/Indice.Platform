import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, inject, viewChild } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideLink, LucideTrash, LucideUpload } from '@lucide/angular';
import { FieldWrapperComponent } from './field-wrapper.component';
import { TailwindControl } from './tailwind-control';

type ImageSource = 'file' | 'url';

/** Default of `options.maxSize`: the largest image file (in bytes) stored inline as a `data:` uri. */
export const defaultImageMaxSize = 1024 * 1024;

/** Removes HTML-like tags in a repeat-until-stable way to avoid incomplete multi-character sanitization. */
function stripHtmlTags(input: string): string {
    let previous: string;
    let current = input;
    do {
        previous = current;
        current = current.replace(/<[^>]*>/g, '');
    } while (current !== previous);
    return current;
}

/** Whether the value is an absolute `https://` url. */
export function isHttpsUrl(value: unknown): value is string {
    if (typeof value !== 'string') {
        return false;
    }
    try {
        const url = new URL(value);
        return url.protocol === 'https:' && !!url.hostname;
    } catch {
        return false;
    }
}

/** Whether the value can be shown as an image: a `data:image/...` uri or an `https://` url. */
export function isImageUri(value: unknown): value is string {
    return typeof value === 'string' && (/^data:image\/[a-z0-9.+-]+(;[^,]*)?,/i.test(value) || isHttpsUrl(value));
}

function formatBytes(bytes: number): string {
    if (bytes >= 1024 * 1024) {
        return `${+(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }
    return bytes >= 1024 ? `${+(bytes / 1024).toFixed(1)} KB` : `${bytes} B`;
}

/**
 * `imageUri` widget for string fields: the image is either picked from a file (`image/*`, stored inline as a `data:` uri, up to
 * `options.maxSize` bytes, 1 MB by default) or given as an external `https://` url. The current image is previewed; read-only shows the image.
 */
@Component({
    selector: 'app-jf-image-uri',
    imports: [FieldWrapperComponent, TranslatePipe, LucideLink, LucideTrash, LucideUpload],
    template: `
    @if (shown) {
      <app-jf-field [control]="this">
        @if (!readonly) {
          <div role="tablist" class="tabs tabs-box tabs-sm mb-2 w-fit">
            <button type="button" role="tab" class="tab gap-1.5" [class.tab-active]="source === 'file'" [attr.aria-selected]="source === 'file'"
              (click)="setSource('file')">
              <svg lucideUpload class="size-4"></svg>{{ 'jsonforms.imageUri.file' | translate }}
            </button>
            <button type="button" role="tab" class="tab gap-1.5" [class.tab-active]="source === 'url'" [attr.aria-selected]="source === 'url'"
              (click)="setSource('url')">
              <svg lucideLink class="size-4"></svg>{{ 'jsonforms.imageUri.url' | translate }}
            </button>
          </div>
          @if (source === 'file') {
            <input
              #fileInput
              type="file"
              accept="image/*"
              [class]="fieldClass + ' max-w-md'"
              [attr.required]="(options.required && !data) || null"
              [id]="id"
              (change)="onFileSelect($event)">
          } @else {
            <input
              type="url"
              [class]="fieldClass"
              [attr.placeholder]="options.placeholder || 'https://'"
              [attr.required]="(options.required && !data) || null"
              [id]="id"
              [value]="urlText"
              (input)="onUrlInput($any($event.target).value)">
          }
          @if (sourceError) {
            <p class="text-xs text-error" role="alert">{{ sourceError.key | translate: sourceError.params }}</p>
          }
        }
        @if (hasImage) {
          <div class="mt-2 flex items-start gap-2">
            @if (data !== brokenSrc) {
              <img [src]="data" [alt]="altText" class="max-h-48 max-w-full rounded-box border border-base-300 bg-base-200/50 object-contain"
                (error)="brokenSrc = data">
            } @else {
              <p class="text-sm text-warning">{{ 'jsonforms.imageUri.loadError' | translate }}</p>
            }
            @if (!readonly) {
              <button type="button" class="btn btn-ghost btn-sm btn-square text-error" (click)="onRemove()"
                [attr.aria-label]="'jsonforms.imageUri.remove' | translate" [title]="'jsonforms.imageUri.remove' | translate">
                <svg lucideTrash class="size-4"></svg>
              </button>
            }
          </div>
        } @else if (readonly) {
          <p class="min-h-10 rounded-field border border-base-300 bg-base-200/50 px-3 py-2 text-sm text-base-content/50">—</p>
        }
      </app-jf-field>
    }`,
    changeDetection: ChangeDetectionStrategy.Eager
})
export class ImageUriControlComponent extends TailwindControl {
    protected widget = 'imageUri';
    protected override fieldBase = 'file-input';
    override readonlyAsText = false;
    private changeDetector = inject(ChangeDetectorRef);
    private fileInput = viewChild<ElementRef<HTMLInputElement>>('fileInput');
    source: ImageSource = 'file';
    /** The text of the url input; kept apart from the value, which only holds a valid url. */
    urlText = '';
    sourceError: { key: string, params?: Record<string, string> } | null = null;
    /** The value whose image failed to load. */
    brokenSrc: string | null = null;
    private sourceChosen = false;

    get hasImage(): boolean {
        return isImageUri(this.data);
    }

    get altText(): string {
        return this.options.alt || stripHtmlTags(this.title ?? '').trim();
    }

    get maxSize(): number {
        const maxSize = Number(this.options.maxSize);
        return maxSize > 0 ? maxSize : defaultImageMaxSize;
    }

    protected override onStateChange(): void {
        if (!this.sourceChosen) {
            this.source = isHttpsUrl(this.data) ? 'url' : 'file';
        }
        if (isHttpsUrl(this.data) && this.data !== this.urlText.trim()) {
            this.urlText = this.data;
        }
        this.fieldBase = this.source === 'file' ? 'file-input' : 'input';
    }

    setSource(source: ImageSource): void {
        this.sourceChosen = true;
        this.source = source;
        this.sourceError = null;
        this.fieldBase = source === 'file' ? 'file-input' : 'input';
    }

    onFileSelect(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0];
        this.sourceError = null;
        if (!file) {
            return;
        }
        if (!file.type.startsWith('image/')) {
            input.value = '';
            this.sourceError = { key: 'jsonforms.imageUri.invalidType' };
            return;
        }
        if (file.size > this.maxSize) {
            input.value = '';
            this.sourceError = { key: 'jsonforms.imageUri.tooLarge', params: { size: formatBytes(this.maxSize) } };
            return;
        }
        const reader = new FileReader();
        reader.onload = () => {
            this.setValue(reader.result as string);
            this.changeDetector.markForCheck();
        };
        reader.onerror = () => {
            input.value = '';
            this.sourceError = { key: 'jsonforms.imageUri.readError' };
            this.changeDetector.markForCheck();
        };
        reader.readAsDataURL(file);
    }

    onUrlInput(text: string): void {
        this.urlText = text;
        const url = text.trim();
        const valid = url === '' || isHttpsUrl(url);
        this.sourceError = valid ? null : { key: 'jsonforms.imageUri.invalidUrl' };
        this.setValue(valid ? url : undefined);
    }

    onRemove(): void {
        const fileInput = this.fileInput()?.nativeElement;
        if (fileInput) {
            fileInput.value = '';
        }
        this.urlText = '';
        this.sourceError = null;
        this.setValue(undefined);
    }
}
