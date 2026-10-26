import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { filter, switchMap } from 'rxjs';
import { LucideMonitorSmartphone, LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { DeviceInfo, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { BoolIconComponent, StatusBadgeComponent } from 'src/app/ui/badges/badges';
import { CopyButtonComponent } from 'src/app/ui/copy/copy-button.component';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { UserStore } from '../user-store';

const BROWSERS = ['brave', 'chrome', 'edge', 'firefox', 'opera', 'postman', 'safari', 'tor'];

/** Devices (browsers and native apps) the user has registered. */
@Component({
  selector: 'app-user-devices',
  imports: [DatePipe, BoolIconComponent, StatusBadgeComponent, CopyButtonComponent, EmptyStateComponent, LucideMonitorSmartphone, LucideTrash],
  template: `
    <p class="mb-3 text-sm text-base-content/70">Browsers and apps registered by the user, used for trusted device and push notification flows.</p>
    <div class="panel overflow-x-auto">
      <table class="table">
        <thead>
          <tr>
            <th>Device</th>
            <th>Platform</th>
            <th class="hidden lg:table-cell">Created</th>
            <th>Last sign-in</th>
            <th class="text-center">Trusted</th>
            <th class="hidden text-center md:table-cell">Push</th>
            <th class="hidden text-center xl:table-cell">Fingerprint</th>
            <th class="hidden text-center xl:table-cell">PIN</th>
            @if (canEdit) { <th class="w-12"><span class="sr-only">Delete</span></th> }
          </tr>
        </thead>
        <tbody>
          @if (items() === null) {
            @for (row of [1, 2]; track row) { <tr><td colspan="9"><div class="skeleton h-4 w-full"></div></td></tr> }
          } @else {
            @for (device of items(); track device.deviceId) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <div class="flex items-center gap-3">
                    <img class="size-6 shrink-0 object-contain" [src]="agentIcon(device)" alt="" />
                    <div class="min-w-0">
                      <p class="flex items-center gap-2 font-medium">
                        {{ device.name || device.model || device.userAgentFamily || 'Unnamed device' }}
                        @if (device.blocked) { <app-status-badge tone="error">Blocked</app-status-badge> }
                      </p>
                      <p class="flex items-center gap-1 text-xs text-base-content/60">
                        <span class="max-w-48 truncate font-mono">{{ device.deviceId }}</span><app-copy-button [value]="device.deviceId" label="device id" />
                      </p>
                    </div>
                  </div>
                </td>
                <td>
                  <span class="flex items-center gap-2 whitespace-nowrap">
                    <img class="size-4 object-contain" [src]="'assets/img/os/' + (device.platform || 'none').toLowerCase() + '.svg'" alt=""
                      (error)="$any($event.target).hidden = true" />
                    {{ device.platform }} {{ device.osVersion }}
                  </span>
                  <span class="text-xs text-base-content/60">{{ device.clientType }}</span>
                </td>
                <td class="hidden tabular whitespace-nowrap text-base-content/75 lg:table-cell">{{ device.dateCreated | date: 'short' }}</td>
                <td class="tabular whitespace-nowrap text-base-content/75">{{ device.lastSignInDate ? (device.lastSignInDate | date: 'short') : '—' }}</td>
                <td class="text-center"><app-bool-icon [value]="device.isTrusted" /></td>
                <td class="hidden text-center md:table-cell"><app-bool-icon [value]="device.isPushNotificationsEnabled" /></td>
                <td class="hidden text-center xl:table-cell"><app-bool-icon [value]="device.supportsFingerprintLogin" /></td>
                <td class="hidden text-center xl:table-cell"><app-bool-icon [value]="device.supportsPinLogin" /></td>
                @if (canEdit) {
                  <td class="text-right">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" aria-label="Delete device" (click)="remove(device)">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td colspan="9">
                  <app-empty-state title="No devices" message="This user has not registered any device.">
                    <svg icon lucideMonitorSmartphone class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          }
        </tbody>
      </table>
    </div>
  `
})
export class UserDevicesComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _store = inject(UserStore);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();
  protected readonly items = signal<DeviceInfo[] | null>(null);

  public ngOnInit(): void {
    this._api.getUserDevices(this._store.id()).subscribe({
      next: result => this.items.set(result.items ?? []),
      error: () => this.items.set([])
    });
  }

  protected agentIcon(device: DeviceInfo): string {
    const family = (device.userAgentFamily ?? '').toLowerCase();
    if (family.includes('samsung') && family.includes('internet')) {
      return 'assets/img/user-agent/samsung-internet.svg';
    }
    if (family === 'ie') {
      return 'assets/img/user-agent/ie.svg';
    }
    const browser = BROWSERS.find(name => family.includes(name));
    if (browser) {
      return `assets/img/user-agent/${browser}.svg`;
    }
    return device.clientType === 'Native' ? 'assets/img/user-agent/generic-phone.svg' : 'assets/img/user-agent/generic-browser.svg';
  }

  protected remove(device: DeviceInfo): void {
    this._dialog
      .confirm({
        title: 'Delete device?',
        message: `'${device.name || device.deviceId}' will be removed from ${this._store.displayName()}'s devices.`,
        confirmLabel: 'Delete',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteUserDevice(this._store.id(), device.deviceId!)))
      .subscribe(() => {
        this.items.update(items => (items ?? []).filter(item => item.deviceId !== device.deviceId));
        this._toast.success('Device deleted');
      });
  }
}
