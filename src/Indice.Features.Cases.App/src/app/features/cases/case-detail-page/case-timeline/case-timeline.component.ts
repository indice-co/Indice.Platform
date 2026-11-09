import { Component, inject, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideDownload, LucideGlobe, LucideMessageSquare, LucideMilestone, LucidePaperclip } from '@lucide/angular';
import { CaseStatus, CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';
import { previewBlob } from 'src/app/shared/services/attachment-preview';

/**
 * The case history: checkpoints reached, comments and attachments, as a vertical rail.
 * Projected content (e.g. the comment composer) sits between the heading and the rail.
 */
@Component({
  selector: 'app-case-timeline',
  imports: [DatePipe, TranslatePipe, LucideDownload, LucideGlobe, LucideMessageSquare, LucideMilestone, LucidePaperclip],
  host: { class: 'block' },
  template: `
    <section class="panel p-5">
      <h2 class="mb-4 text-sm font-semibold">{{ 'caseDetails.timeline.title' | translate }}</h2>
      <div class="mb-5 empty:hidden"><ng-content /></div>
      @if (timelineEntries().length) {
        <ol class="relative space-y-5 before:absolute before:top-2 before:bottom-2 before:left-[0.9375rem] before:w-px before:bg-base-300">
          @for (entry of timelineEntries(); track $index) {
            @let hasAttachment = !!entry.comment?.attachment?.id;
            <li class="relative flex gap-3">
              <span class="relative z-10 grid size-8 shrink-0 place-items-center rounded-full border border-base-300 bg-base-100"
                [class.text-primary]="entry.isCheckpoint">
                @if (entry.isCheckpoint) {
                  <svg lucideMilestone class="size-4"></svg>
                } @else if (hasAttachment) {
                  <svg lucidePaperclip class="size-4"></svg>
                } @else {
                  <svg lucideMessageSquare class="size-4"></svg>
                }
              </span>
              <div class="min-w-0 flex-1 pt-1">
                <div class="flex flex-wrap items-center gap-1.5">
                  <span class="text-sm font-semibold">
                    @if (entry.isCheckpoint) {
                      {{ entry.checkpoint?.checkpointType?.title ?? entry.checkpoint?.checkpointType?.code }}
                    } @else if (hasAttachment) {
                      {{ 'caseDetails.timeline.attachment' | translate }}
                    } @else if (entry.comment) {
                      {{ 'common.comment' | translate }}
                    }
                  </span>
                  @if (entry.checkpoint?.checkpointType?.private) {
                    <span class="badge badge-xs badge-ghost">{{ 'caseDetails.timeline.private' | translate }}</span>
                  }
                  @if (entry.comment?.isCustomer) {
                    <span class="badge badge-xs badge-info badge-soft">{{ 'caseDetails.timeline.customer' | translate }}</span>
                  } @else if (entry.comment && !hasAttachment && entry.comment.private === false) {
                    <span class="badge badge-xs badge-info badge-soft gap-0.5" [title]="'caseDetails.comments.publicHint' | translate">
                      <svg lucideGlobe class="size-3"></svg>{{ 'caseDetails.comments.public' | translate }}
                    </span>
                  }
                  @let status = entry.checkpoint?.checkpointType?.status;
                  @if (status && !hideStatuses()) {
                    <span class="stamp" [class]="statusClass(status)" [title]="'caseDetails.timeline.publicStatus' | translate">
                      {{ ('caseStatus.' + status) | translate }}
                    </span>
                  }
                </div>
                <p class="mt-0.5 text-xs text-base-content/60">
                  <span [title]="entry.createdBy?.email ?? ''">{{ entry.createdBy?.name }}</span>,
                  <time [attr.datetime]="entry.timestamp | date: 'yyyy-MM-ddTHH:mm:ss'">{{ entry.timestamp | date: 'dd/MM/yy, HH:mm' }}</time>
                </p>
                @if (entry.comment?.text && !hasAttachment) {
                  <p class="mt-2 whitespace-pre-line rounded-field bg-base-200 px-3 py-2 text-sm">{{ entry.comment?.text }}</p>
                }
                @if (hasAttachment) {
                  <button type="button" class="btn btn-sm mt-2" (click)="onAttachmentClick(entry.comment!.attachment!.id!)">
                    <svg lucideDownload class="size-4"></svg>
                    {{ 'common.download' | translate }}
                  </button>
                }
              </div>
            </li>
          }
        </ol>
      } @else {
        <p class="text-sm text-base-content/65">{{ 'caseDetails.timeline.empty' | translate }}</p>
      }
    </section>
  `
})
export class CaseTimelineComponent {
  private readonly _api = inject(CasesApiService);
  public readonly timelineEntries = input<TimelineEntry[]>([]);
  public readonly hideStatuses = input(false);

  protected statusClass(status: CaseStatus): string {
    return {
      [CaseStatus.Submitted]: 'stamp-submitted',
      [CaseStatus.InProgress]: 'stamp-inprogress',
      [CaseStatus.Completed]: 'stamp-completed',
      [CaseStatus.Deleted]: 'stamp-deleted'
    }[status] ?? 'stamp-neutral';
  }

  protected onAttachmentClick(attachmentId: string): void {
    this._api.downloadAttachment(attachmentId).subscribe(result => previewBlob(result.data, result.fileName ?? attachmentId));
  }
}
