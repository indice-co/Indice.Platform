import { Component, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { finalize } from 'rxjs/operators';
import { LucideDownload, LucideExternalLink, LucideFileText, LucidePaperclip, LucideTriangleAlert } from '@lucide/angular';
import { Case, CaseAttachment, CasesApiService } from 'src/app/core/services/cases-api.service';
import { canPreview, previewBlob, saveBlob } from 'src/app/shared/services/attachment-preview';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';

/** Flat list of every file attached to the case, through the form or the comments. Reloads whenever the case is refreshed. */
@Component({
  selector: 'app-case-attachments',
  imports: [TranslatePipe, EmptyStateComponent, LucideDownload, LucideExternalLink, LucideFileText, LucidePaperclip, LucideTriangleAlert],
  host: { class: 'block' },
  template: `
    @if (attachments.error()) {
      <app-empty-state [title]="'caseDetails.attachments.title' | translate" [message]="'caseDetails.attachments.loadError' | translate">
        <svg icon lucideTriangleAlert class="size-6"></svg>
        <button actions type="button" class="btn btn-sm" (click)="attachments.reload()">{{ 'common.retry' | translate }}</button>
      </app-empty-state>
    } @else if (!attachments.hasValue()) {
      <div class="grid place-items-center py-14" aria-busy="true"><span class="loading loading-spinner"></span></div>
    } @else if (!items().length) {
      <app-empty-state [title]="'caseDetails.attachments.empty' | translate" [message]="'caseDetails.attachments.emptyMessage' | translate">
        <svg icon lucidePaperclip class="size-6"></svg>
      </app-empty-state>
    } @else {
      <div class="max-h-96 overflow-auto">
        <table class="table table-sm table-pin-rows">
          <thead>
            <tr class="bg-base-100 text-base-content/70">
              <th class="font-medium whitespace-nowrap">{{ 'caseDetails.attachments.fileName' | translate }}</th>
              <th class="font-medium whitespace-nowrap">{{ 'caseDetails.attachments.type' | translate }}</th>
              <th><span class="sr-only">{{ 'common.actions' | translate }}</span></th>
            </tr>
          </thead>
          <tbody>
            @for (attachment of items(); track attachment.id) {
              @let busy = pending() === attachment.id;
              <tr class="hover:bg-base-200">
                <td class="max-w-0 w-full">
                  <span class="flex items-center gap-2">
                    <svg lucideFileText class="size-4 shrink-0 text-base-content/60"></svg>
                    <span class="truncate" [title]="attachment.fileName ?? ''">{{ attachment.fileName || '-' }}</span>
                  </span>
                </td>
                <td class="whitespace-nowrap">
                  <span class="badge badge-sm badge-ghost docket" [title]="attachment.contentType ?? ''">{{ extension(attachment) }}</span>
                </td>
                <td class="whitespace-nowrap text-right">
                  <div class="join">
                    @if (previewable(attachment)) {
                      <button type="button" class="btn btn-ghost btn-xs join-item" [disabled]="busy" (click)="open(attachment)"
                        [title]="'common.open' | translate" [attr.aria-label]="('common.open' | translate) + ' ' + attachment.fileName">
                        <svg lucideExternalLink class="size-4"></svg>
                      </button>
                    }
                    <button type="button" class="btn btn-ghost btn-xs join-item" [disabled]="busy" (click)="download(attachment)"
                      [title]="'common.download' | translate" [attr.aria-label]="('common.download' | translate) + ' ' + attachment.fileName">
                      @if (busy) { <span class="loading loading-spinner loading-xs"></span> } @else { <svg lucideDownload class="size-4"></svg> }
                    </button>
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `
})
export class CaseAttachmentsComponent {
  private readonly _api = inject(CasesApiService);

  /** The case whose attachments are listed; a new instance (a refresh) reloads the list. */
  public readonly case = input<Case | undefined>();

  protected readonly attachments = rxResource({
    params: () => this.case(),
    stream: ({ params }) => this._api.getCaseAttachments(params.id!)
  });
  protected readonly pending = signal<string | undefined>(undefined);

  protected items(): CaseAttachment[] {
    return this.attachments.value()?.items ?? [];
  }

  protected extension(attachment: CaseAttachment): string {
    return (attachment.fileExtension ?? '').replace(/^\./, '').toUpperCase() || '-';
  }

  protected previewable(attachment: CaseAttachment): boolean {
    return canPreview(attachment.contentType);
  }

  protected open(attachment: CaseAttachment): void {
    this.fetch(attachment, previewBlob);
  }

  protected download(attachment: CaseAttachment): void {
    this.fetch(attachment, saveBlob);
  }

  private fetch(attachment: CaseAttachment, use: (blob: Blob, fileName: string) => void): void {
    this.pending.set(attachment.id);
    this._api.downloadAttachment(attachment.id!)
      .pipe(finalize(() => this.pending.set(undefined)))
      .subscribe(result => use(result.data, attachment.fileName ?? attachment.id!));
  }
}
