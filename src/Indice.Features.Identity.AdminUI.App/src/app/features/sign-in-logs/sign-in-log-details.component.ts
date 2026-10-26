import { Component, inject, signal } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { LucideExternalLink, LucideFlag, LucideFlagOff, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { IdentityApiService, SignInLogEntry, SignInLogEntryRequest } from 'src/app/core/services/identity-api.service';
import { StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DIALOG_DATA, DialogRef } from 'src/app/ui/dialog/dialog.service';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { LogDeviceComponent, LogFieldComponent, LogIpComponent, idpIcon, relativeTime } from './log-parts';

/** Dialog with every field of a sign in log entry. Closes with `true` when the review flag changed. */
@Component({
  selector: 'app-sign-in-log-details',
  imports: [
    DatePipe, JsonPipe, StatusBadgeComponent, CopyButtonComponent, LogDeviceComponent, LogFieldComponent, LogIpComponent,
    LucideExternalLink, LucideFlag, LucideFlagOff, LucideTriangleAlert, LucideX
  ],
  template: `
    <div class="flex items-start justify-between gap-4">
      <div class="min-w-0">
        <div class="flex flex-wrap items-center gap-2">
          <h3 class="text-lg font-semibold">{{ entry.actionName || 'Sign in event' }}</h3>
          <app-status-badge [tone]="entry.succeeded ? 'success' : 'error'">{{ entry.succeeded ? 'Succeeded' : 'Failed' }}</app-status-badge>
          @if (review()) { <app-status-badge tone="warning">Marked for review</app-status-badge> }
          @if (entry.extraData?.warning) { <app-status-badge tone="warning">{{ entry.extraData?.warning }}</app-status-badge> }
        </div>
        <p class="mt-1 text-sm text-base-content/65">{{ entry.createdAt | date: 'medium' }} · {{ relative }}</p>
      </div>
      <button type="button" class="btn btn-ghost btn-sm btn-square" (click)="ref.close(changed)" aria-label="Close">
        <svg lucideX class="size-5"></svg>
      </button>
    </div>

    @if (entry.extraData?.error || entry.description) {
      <div role="alert" class="alert mt-4" [class.alert-error]="!entry.succeeded" [class.alert-soft]="true">
        @if (!entry.succeeded) { <svg lucideTriangleAlert class="size-5"></svg> }
        <div class="min-w-0">
          @if (entry.extraData?.error) { <p class="font-semibold">{{ entry.extraData?.error }}</p> }
          @if (entry.extraData?.errorDescription) { <p class="text-sm">{{ entry.extraData?.errorDescription }}</p> }
          @if (entry.description) { <p class="text-sm">{{ entry.description }}</p> }
        </div>
      </div>
    }

    <div class="mt-4 grid gap-x-8 lg:grid-cols-2">
      <section>
        <h4 class="mb-1 text-xs font-semibold tracking-wider text-base-content/50 uppercase">Who & where</h4>
        <dl class="divide-y divide-base-200">
          <app-log-field label="Subject">
            <span [class.text-error]="entry.subjectUnknown">{{ entry.subjectName || '—' }}</span>
            @if (entry.subjectUnknown) { <span class="ml-1 text-xs text-error">(unknown user)</span> }
          </app-log-field>
          <app-log-field label="Subject id">
            @if (entry.subjectId) { <span class="code-chip">{{ entry.subjectId }}</span><app-copy-button [value]="entry.subjectId" label="subject id" /> } @else { — }
          </app-log-field>
          <app-log-field label="Application">
            {{ entry.applicationName || '—' }}
            @if (entry.applicationId) { <span class="code-chip ml-1">{{ entry.applicationId }}</span> }
          </app-log-field>
          <app-log-field label="Identity provider">
            <span class="inline-flex items-center gap-2">
              @if (idp) { <img [src]="idp" class="h-4" alt="" /> }
              {{ entry.extraData?.provider || 'local' }}
            </span>
          </app-log-field>
          <app-log-field label="IP address"><app-log-ip [ip]="entry.ipAddress" [country]="entry.countryIsoCode" /></app-log-field>
          <app-log-field label="Location">
            {{ entry.location || '—' }}
            @if (entry.countryIsoCode) { <span class="ml-1 text-base-content/60">({{ entry.countryIsoCode }})</span> }
          </app-log-field>
          <app-log-field label="Coordinates">
            @if (entry.coordinates) {
              <a class="link link-primary inline-flex items-center gap-1" target="_blank" rel="noopener"
                [href]="'https://www.google.com/maps/search/?api=1&query=' + entry.coordinates">
                {{ entry.coordinates }}<svg lucideExternalLink class="size-3.5"></svg>
              </a>
            } @else { — }
          </app-log-field>
          <app-log-field label="Device">
            <app-log-device [displayName]="entry.extraData?.device?.displayName" [userAgentFamily]="entry.extraData?.device?.userAgentFamily"
              [platform]="entry.extraData?.device?.platform" />
          </app-log-field>
          @if (entry.extraData?.device?.userAgent) {
            <app-log-field label="User agent"><span class="font-mono text-xs">{{ entry.extraData?.device?.userAgent }}</span></app-log-field>
          }
        </dl>
      </section>
      <section>
        <h4 class="mb-1 text-xs font-semibold tracking-wider text-base-content/50 uppercase">Request</h4>
        <dl class="divide-y divide-base-200">
          <app-log-field label="Event type">{{ entry.eventType || '—' }}</app-log-field>
          <app-log-field label="Sign-in type">{{ entry.signInType || '—' }}</app-log-field>
          <app-log-field label="Grant type">{{ entry.grantType || '—' }}</app-log-field>
          <app-log-field label="Endpoint">{{ entry.resourceId || '—' }}</app-log-field>
          <app-log-field label="Resource type">{{ entry.resourceType || '—' }}</app-log-field>
          <app-log-field label="Scope">{{ entry.extraData?.scope || '—' }}</app-log-field>
          <app-log-field label="Redirect uri"><span class="break-all">{{ entry.extraData?.redirectUri || '—' }}</span></app-log-field>
          @if (entry.extraData?.authenticationMethods?.length) {
            <app-log-field label="Auth methods">{{ entry.extraData?.authenticationMethods?.join(', ') }}</app-log-field>
          }
          <app-log-field label="Session id">
            @if (entry.sessionId) { <span class="code-chip">{{ entry.sessionId }}</span><app-copy-button [value]="entry.sessionId" label="session id" /> } @else { — }
          </app-log-field>
          <app-log-field label="Request id">
            @if (entry.requestId) { <span class="code-chip">{{ entry.requestId }}</span><app-copy-button [value]="entry.requestId" label="request id" /> } @else { — }
          </app-log-field>
          <app-log-field label="Device id">
            @if (entry.deviceId) { <span class="code-chip">{{ entry.deviceId }}</span> } @else { — }
          </app-log-field>
          <app-log-field label="Entry id"><span class="code-chip">{{ entry.id }}</span></app-log-field>
        </dl>
      </section>
    </div>

    @if (entry.extraData?.tokens?.length) {
      <section class="mt-4">
        <h4 class="mb-2 text-xs font-semibold tracking-wider text-base-content/50 uppercase">Tokens</h4>
        <ul class="space-y-2">
          @for (token of entry.extraData?.tokens; track $index) {
            <li class="rounded-field bg-base-200 p-3">
              <div class="mb-1 flex items-center justify-between gap-2">
                <span class="badge badge-sm badge-ghost">{{ token.tokenType }}</span>
                <app-copy-button [value]="token.tokenValue" label="token" />
              </div>
              <p class="font-mono text-xs break-all text-base-content/80">{{ token.tokenValue }}</p>
            </li>
          }
        </ul>
      </section>
    }

    <details class="collapse-arrow collapse mt-4 border border-base-300 bg-base-100">
      <summary class="collapse-title text-sm font-semibold">Raw metadata</summary>
      <div class="collapse-content">
        <pre class="max-h-80 overflow-auto rounded-field bg-base-200 p-3 font-mono text-xs">{{ entry.extraData | json }}</pre>
      </div>
    </details>

    <div class="modal-action">
      @if (canReview) {
        <button type="button" class="btn btn-ghost" [disabled]="saving()" (click)="toggleReview()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          @else if (review()) { <svg lucideFlagOff class="size-4"></svg> } @else { <svg lucideFlag class="size-4"></svg> }
          {{ review() ? 'Clear review flag' : 'Mark for review' }}
        </button>
      }
      <button type="button" class="btn btn-primary" (click)="ref.close(changed)">Close</button>
    </div>
  `
})
export class SignInLogDetailsComponent {
  private readonly _api = inject(IdentityApiService);
  private readonly _toast = inject(ToastService);
  protected readonly entry = inject<SignInLogEntry>(DIALOG_DATA);
  protected readonly ref = inject<DialogRef<boolean>>(DialogRef);
  protected readonly canReview = inject(AuthService).isAdmin();
  protected readonly review = signal(!!this.entry.review);
  protected readonly saving = signal(false);
  protected readonly relative = relativeTime(this.entry.createdAt);
  protected readonly idp = idpIcon(this.entry.extraData?.provider);
  protected changed = false;

  protected toggleReview(): void {
    const next = !this.review();
    this.saving.set(true);
    this._api.patchSignInLog(this.entry.id!, new SignInLogEntryRequest({ review: next })).subscribe({
      next: () => {
        this.review.set(next);
        this.entry.review = next;
        this.changed = true;
        this.saving.set(false);
        this._toast.success(next ? 'Marked for review' : 'Review flag cleared');
      },
      error: () => this.saving.set(false)
    });
  }
}
