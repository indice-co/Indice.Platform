import { Component, LOCALE_ID, inject, signal } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { LucideExternalLink, LucideFlag, LucideFlagOff, LucideX } from '@lucide/angular';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { ActivityLogEntry, ActivityLogEntryRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DIALOG_DATA, DialogRef } from 'src/app/ui/dialog/dialog.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { LogDeviceComponent, LogFieldComponent, LogIpComponent, relativeTime } from '../sign-in-logs/log-parts';

/** Dialog with every field of an activity log entry. Closes with `true` when the review flag changed. */
@Component({
  selector: 'app-activity-log-details',
  imports: [
    DatePipe, JsonPipe, TranslatePipe, StatusBadgeComponent, CopyButtonComponent, LogDeviceComponent, LogFieldComponent, LogIpComponent,
    LucideExternalLink, LucideFlag, LucideFlagOff, LucideX
  ],
  template: `
    <div class="flex items-start justify-between gap-4">
      <div class="min-w-0">
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="text-lg font-semibold">{{ entry.actionName || ('ActivityLogs.Details.DefaultTitle' | translate) }}</h3>
          <app-status-badge [tone]="entry.succeeded ? 'success' : 'error'">{{ (entry.succeeded ? 'Logs.Outcome.Succeeded' : 'Logs.Outcome.Failed') | translate }}</app-status-badge>
          @if (review()) { <app-status-badge tone="warning">{{ 'Logs.Review.Marked' | translate }}</app-status-badge> }
        </div>
        <p class="mt-1 text-sm text-base-content/65">{{ entry.createdAt | date: 'medium' }} · {{ relative }}</p>
      </div>
      <div class="flex shrink-0 items-center gap-2">
        @if (canReview) {
          <button type="button" class="btn btn-sm" [class.btn-warning]="review()" [class.btn-soft]="review()" [disabled]="saving()"
            [attr.aria-pressed]="review()" (click)="toggleReview()">
            @if (saving()) { <span class="loading loading-spinner loading-xs"></span> }
            @else if (review()) { <svg lucideFlagOff class="size-4"></svg> } @else { <svg lucideFlag class="size-4"></svg> }
            {{ (review() ? 'Logs.Review.Clear' : 'Logs.Review.Mark') | translate }}
          </button>
        }
        <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="ref.close(changed)" [attr.aria-label]="'Common.Close' | translate">
          <svg lucideX class="size-5"></svg>
        </button>
      </div>
    </div>

    @if (entry.description) {
      <p class="mt-4 rounded-field bg-base-200 px-4 py-3 text-sm">{{ entry.description }}</p>
    }

    <div class="mt-4 grid gap-x-8 lg:grid-cols-2">
      <section>
        <h4 class="mb-1 text-xs font-semibold tracking-wider text-base-content/50 uppercase">{{ 'ActivityLogs.Details.ActorAndOrigin' | translate }}</h4>
        <dl class="divide-y divide-base-200">
          <app-log-field [label]="'Logs.Fields.Subject' | translate">
            <span [class.text-error]="entry.subjectUnknown">{{ entry.subjectName || '—' }}</span>
            @if (entry.subjectUnknown) { <span class="ml-1 text-xs text-error">{{ 'Logs.Fields.UnknownUser' | translate }}</span> }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.SubjectId' | translate">
            @if (entry.subjectId) { <span class="code-chip">{{ entry.subjectId }}</span><app-copy-button [value]="entry.subjectId" [label]="'Logs.Copy.SubjectId' | translate" /> } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.Application' | translate">
            {{ entry.applicationName || '—' }}
            @if (entry.applicationId) { <span class="code-chip ml-1">{{ entry.applicationId }}</span> }
          </app-log-field>
          <app-log-field [label]="'ActivityLogs.Fields.Source' | translate">{{ entry.source || '—' }}</app-log-field>
          <app-log-field [label]="'Logs.Fields.IpAddress' | translate"><app-log-ip [ip]="entry.ipAddress" [country]="entry.countryIsoCode" /></app-log-field>
          <app-log-field [label]="'Logs.Fields.Location' | translate">
            {{ entry.location || '—' }}
            @if (entry.countryIsoCode) { <span class="ml-1 text-base-content/60">({{ entry.countryIsoCode }})</span> }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.Coordinates' | translate">
            @if (entry.coordinates) {
              <a class="link link-primary inline-flex items-center gap-1" target="_blank" rel="noopener"
                [href]="'https://www.google.com/maps/search/?api=1&query=' + entry.coordinates">
                {{ entry.coordinates }}<svg lucideExternalLink class="size-3.5"></svg>
              </a>
            } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.Device' | translate">
            <app-log-device [displayName]="entry.extraData?.device?.displayName" [userAgentFamily]="entry.extraData?.device?.userAgentFamily"
              [platform]="entry.extraData?.device?.platform" />
          </app-log-field>
          @if (entry.extraData?.device?.userAgent) {
            <app-log-field [label]="'Logs.Fields.UserAgent' | translate"><span class="font-mono text-xs">{{ entry.extraData?.device?.userAgent }}</span></app-log-field>
          }
        </dl>
      </section>
      <section>
        <h4 class="mb-1 text-xs font-semibold tracking-wider text-base-content/50 uppercase">{{ 'ActivityLogs.Details.Event' | translate }}</h4>
        <dl class="divide-y divide-base-200">
          <app-log-field [label]="'Logs.Fields.EventType' | translate">{{ entry.eventType || '—' }}</app-log-field>
          <app-log-field [label]="'ActivityLogs.Fields.Category' | translate">{{ entry.category || '—' }}</app-log-field>
          <app-log-field [label]="'Logs.Fields.ResourceType' | translate">{{ entry.resourceType || '—' }}</app-log-field>
          <app-log-field [label]="'ActivityLogs.Fields.ResourceId' | translate">
            @if (entry.resourceId) { <span class="code-chip">{{ entry.resourceId }}</span><app-copy-button [value]="entry.resourceId" [label]="'Logs.Copy.ResourceId' | translate" /> } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.SessionId' | translate">
            @if (entry.sessionId) { <span class="code-chip">{{ entry.sessionId }}</span><app-copy-button [value]="entry.sessionId" [label]="'Logs.Copy.SessionId' | translate" /> } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.RequestId' | translate">
            @if (entry.requestId) { <span class="code-chip">{{ entry.requestId }}</span><app-copy-button [value]="entry.requestId" [label]="'Logs.Copy.RequestId' | translate" /> } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.DeviceId' | translate">
            @if (entry.deviceId) { <span class="code-chip">{{ entry.deviceId }}</span> } @else { — }
          </app-log-field>
          <app-log-field [label]="'Logs.Fields.EntryId' | translate"><span class="code-chip">{{ entry.id }}</span></app-log-field>
        </dl>
      </section>
    </div>

    @if (entry.extraData?.extraData) {
      <section class="mt-4">
        <h4 class="mb-2 text-xs font-semibold tracking-wider text-base-content/50 uppercase">{{ 'ActivityLogs.Details.ExtraData' | translate }}</h4>
        <pre class="max-h-80 overflow-auto rounded-field bg-base-200 p-3 font-mono text-xs">{{ entry.extraData?.extraData | json }}</pre>
      </section>
    }

    <div class="modal-action">
      <button type="button" class="btn btn-primary" (click)="ref.close(changed)">{{ 'Common.Close' | translate }}</button>
    </div>
  `
})
export class ActivityLogDetailsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  private readonly _translate = inject(TranslateService);
  protected readonly entry = inject<ActivityLogEntry>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  // matches the server's BeLogsWriter policy
  protected readonly canReview = inject(AuthService).isAdminUIUsersWriter();
  protected readonly review = signal(!!this.entry.review);
  protected readonly saving = signal(false);
  protected readonly relative = relativeTime(this.entry.createdAt, inject(LOCALE_ID));
  protected changed = false;

  protected toggleReview(): void {
    const next = !this.review();
    this.saving.set(true);
    this._api.patchActivityLog(this.entry.id!, new ActivityLogEntryRequest({ review: next })).subscribe({
      next: () => {
        this.review.set(next);
        this.entry.review = next;
        this.changed = true;
        this.saving.set(false);
        this._toast.success(this._translate.instant(next ? 'Logs.Review.Marked' : 'Logs.Review.Cleared'));
      },
      error: () => this.saving.set(false)
    });
  }
}
