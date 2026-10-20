import { DatePipe } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CasesApiService, TimelineEntry } from 'src/app/core/services/cases-api.service';
import { inkOf, statusLabelKey } from 'src/app/ui/status/case-status';

/**
 * The case history: checkpoints reached, comments and attachments, newest as returned by the server. Checkpoints are
 * dots of lifecycle ink on the rail; their public status is a pale tag (the stamp belongs to the current checkpoint only).
 */
@Component({
  selector: 'app-case-timeline',
  imports: [DatePipe, NgIcon, TranslatePipe],
  template: `
    <ol class="relative ml-3 border-l-2 border-base-200">
      @for (entry of timelineEntries(); track $index) {
        <li class="relative pb-6 pl-7 last:pb-0">
          <span class="absolute top-0 -left-[0.8125rem] grid size-6 place-items-center rounded-full bg-base-100"
                [class]="entry.checkpoint ? ink(entry) : 'bg-base-200'">
            @if (entry.checkpoint) {
              <span class="size-3 rounded-full bg-[var(--mark)]"></span>
            } @else if (entry.comment?.attachment?.id) {
              <ng-icon name="lucidePaperclip" size="0.8rem" class="text-quiet" />
            } @else {
              <ng-icon name="lucideMessageSquare" size="0.8rem" class="text-quiet" />
            }
          </span>

          <div class="flex flex-wrap items-center gap-x-2 gap-y-1">
            @if (entry.isCheckpoint) {
              <span class="font-semibold">{{ entry.checkpoint?.checkpointType?.title ?? entry.checkpoint?.checkpointType?.code }}</span>
            } @else if (entry.comment?.attachment?.id) {
              <span class="font-semibold">{{ 'caseDetails.timeline.attachment' | translate }}</span>
            } @else if (entry.comment) {
              <span class="font-semibold">{{ 'common.comment' | translate }}</span>
            }
            @if (entry.checkpoint?.checkpointType?.private) {
              <span class="badge badge-sm badge-ghost gap-1"><ng-icon name="lucideLock" size="0.7rem" /> {{ 'caseDetails.timeline.private' | translate }}</span>
            }
            @if (entry.comment?.isCustomer) {
              <span class="badge badge-sm badge-soft badge-secondary">{{ 'caseDetails.timeline.customer' | translate }}</span>
            }
            @if (entry.checkpoint?.checkpointType?.status && !hideStatuses()) {
              <span class="status-pill" [class]="ink(entry)" [title]="'caseDetails.timeline.publicStatus' | translate">
                {{ statusKey(entry) | translate }}
              </span>
            }
          </div>
          <p class="mt-0.5 text-xs text-quiet">
            <span [title]="entry.createdBy?.email ?? ''">{{ entry.createdBy?.name }}</span>,
            <time class="kbd-ref" [attr.datetime]="entry.timestamp?.toISOString()">{{ entry.timestamp | date: 'dd/MM/yy, HH:mm' }}</time>
          </p>

          @if (entry.comment?.text && !entry.comment?.attachment?.id) {
            <p class="mt-2 max-w-prose rounded-field bg-base-200 px-3 py-2 whitespace-pre-line">{{ entry.comment?.text }}</p>
          }
          @if (entry.comment?.attachment?.id) {
            <button type="button" class="btn btn-sm mt-2" (click)="onAttachmentClick(entry.comment!.attachment!.id!)">
              <ng-icon name="lucideDownload" size="0.95rem" /> {{ 'common.download' | translate }}
            </button>
          }
        </li>
      }
    </ol>
  `
})
export class CaseTimelineComponent {
  private readonly api = inject(CasesApiService);
  public readonly timelineEntries = input<TimelineEntry[]>([]);
  public readonly hideStatuses = input(false);

  protected ink(entry: TimelineEntry): string {
    return inkOf(entry.checkpoint?.checkpointType?.status);
  }

  protected statusKey(entry: TimelineEntry): string {
    return statusLabelKey(entry.checkpoint?.checkpointType?.status);
  }

  protected onAttachmentClick(attachmentId: string): void {
    this.api.downloadAttachment(attachmentId).subscribe(results => {
      window.open(window.URL.createObjectURL(results.data), '_blank');
    });
  }
}
