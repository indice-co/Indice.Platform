import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { LucideLink, LucidePlus, LucideRotateCcw, LucideTrash, LucideTriangleAlert } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ORIGIN_PATTERN, URL_PATTERN } from '../client-utils';
import { ClientStore } from './client-store.service';

type UrlKind = 'redirect' | 'cors' | 'postLogout';

interface UrlRow {
  url: string;
  redirect: boolean;
  cors: boolean;
  postLogout: boolean;
}

/**
 * Redirect, CORS and post-logout URLs, edited as one table: each URL can play any of the three roles.
 * Changes are kept locally until saved.
 */
@Component({
  selector: 'app-client-urls',
  imports: [ReactiveFormsModule, EmptyStateComponent, LucideLink, LucidePlus, LucideRotateCcw, LucideTrash, LucideTriangleAlert],
  template: `
    <div class="panel">
      @if (canEdit) {
        <form class="flex flex-col gap-2 border-b border-base-300 p-4 sm:flex-row sm:items-start" (ngSubmit)="add()">
          <div class="flex-1">
            <input class="input w-full font-mono text-sm" [formControl]="newUrl" placeholder="https://app.example.com/signin-oidc"
              [class.input-error]="newUrl.invalid && newUrl.touched" aria-label="URL to add" />
            @if (newUrl.touched && newUrl.hasError('pattern')) {
              <p class="mt-1 text-xs text-error">Enter a valid URL.</p>
            } @else if (newUrl.touched && newUrl.hasError('duplicate')) {
              <p class="mt-1 text-xs text-error">This URL is already listed.</p>
            }
          </div>
          <button type="submit" class="btn btn-primary" [disabled]="!newUrl.value"><svg lucidePlus class="size-4"></svg>Add URL</button>
        </form>
      }
      <div class="overflow-x-auto">
        <table class="table">
          <thead>
            <tr>
              <th>URL</th>
              <th class="w-28 text-center"><span class="tooltip" data-tip="Tokens can be sent to this URL">Redirect</span></th>
              <th class="w-28 text-center"><span class="tooltip" data-tip="Origin allowed to make cross origin requests">CORS</span></th>
              <th class="w-32 text-center"><span class="tooltip tooltip-left" data-tip="Users can return here after signing out">Post logout</span></th>
              @if (canEdit) { <th class="w-12"><span class="sr-only">Remove</span></th> }
            </tr>
          </thead>
          <tbody>
            @for (row of rows(); track row.url) {
              <tr [class.opacity-50]="isRemoved(row)">
                <td class="max-w-xl">
                  <span class="inline-flex items-center gap-2 font-mono text-sm break-all" [class.line-through]="isRemoved(row)">
                    {{ row.url }}
                    @if (row.cors && !isValidOrigin(row.url)) {
                      <span class="tooltip" data-tip="Not a valid CORS origin: use scheme, host and port only">
                        <svg lucideTriangleAlert class="size-4 text-warning"></svg>
                      </span>
                    }
                  </span>
                </td>
                @for (kind of kinds; track kind) {
                  <td class="text-center">
                    <input type="checkbox" class="checkbox checkbox-primary checkbox-sm" [checked]="row[kind]" [disabled]="!canEdit"
                      [attr.aria-label]="kind + ' ' + row.url" (change)="toggle(row, kind, $any($event.target).checked)" />
                  </td>
                }
                @if (canEdit) {
                  <td class="text-right">
                    @if (isRemoved(row)) {
                      <button type="button" class="btn btn-ghost btn-xs btn-square" aria-label="Restore" (click)="restore(row)">
                        <svg lucideRotateCcw class="size-4"></svg>
                      </button>
                    } @else {
                      <button type="button" class="btn btn-ghost btn-xs btn-square text-error" [attr.aria-label]="'Remove ' + row.url" (click)="removeAll(row)">
                        <svg lucideTrash class="size-4"></svg>
                      </button>
                    }
                  </td>
                }
              </tr>
            } @empty {
              <tr>
                <td [attr.colspan]="canEdit ? 5 : 4">
                  <app-empty-state title="No URLs yet" message="Add the addresses this application redirects to after sign in or sign out.">
                    <svg icon lucideLink class="size-5"></svg>
                  </app-empty-state>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      @if (canEdit) {
        <div class="flex items-center justify-end gap-3 border-t border-base-300 px-4 py-3">
          @if (dirty()) { <span class="text-sm text-base-content/60">Unsaved changes</span> }
          <button type="button" class="btn btn-ghost" [disabled]="!dirty() || saving()" (click)="reset()">Discard</button>
          <button type="button" class="btn btn-primary" [disabled]="!dirty() || hasInvalidOrigins() || saving()" (click)="save()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save URLs
          </button>
        </div>
      }
    </div>
  `
})
export class ClientUrlsComponent implements OnInit {
  private readonly _store = inject(ClientStore);
  private readonly _toast = inject(ToastService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();
  protected readonly kinds: UrlKind[] = ['redirect', 'cors', 'postLogout'];
  protected readonly rows = signal<UrlRow[]>([]);
  protected readonly dirty = signal(false);
  protected readonly saving = signal(false);
  protected readonly newUrl = new FormControl('', { nonNullable: true, validators: [Validators.pattern(URL_PATTERN)] });
  protected readonly hasInvalidOrigins = computed(() => this.rows().some(row => row.cors && !this.isValidOrigin(row.url)));

  public ngOnInit(): void {
    this.reset();
  }

  protected reset(): void {
    const client = this._store.client()!;
    const redirect = client.redirectUris ?? [];
    const cors = client.allowedCorsOrigins ?? [];
    const postLogout = client.postLogoutRedirectUris ?? [];
    const urls = [...new Set([...redirect, ...cors, ...postLogout])].sort();
    this.rows.set(urls.map(url => ({ url, redirect: redirect.includes(url), cors: cors.includes(url), postLogout: postLogout.includes(url) })));
    this.dirty.set(false);
  }

  protected isValidOrigin(url: string): boolean {
    return ORIGIN_PATTERN.test(url);
  }

  protected isRemoved(row: UrlRow): boolean {
    return !row.redirect && !row.cors && !row.postLogout;
  }

  protected add(): void {
    const url = this.newUrl.value.trim();
    this.newUrl.markAsTouched();
    if (!url || this.newUrl.invalid) {
      return;
    }
    if (this.rows().some(row => row.url === url)) {
      this.newUrl.setErrors({ duplicate: true });
      return;
    }
    this.rows.update(rows => [...rows, { url, redirect: true, cors: false, postLogout: false }]);
    this.dirty.set(true);
    this.newUrl.reset();
  }

  protected toggle(row: UrlRow, kind: UrlKind, checked: boolean): void {
    this.replace(row, { ...row, [kind]: checked });
  }

  protected removeAll(row: UrlRow): void {
    this.replace(row, { ...row, redirect: false, cors: false, postLogout: false });
  }

  protected restore(row: UrlRow): void {
    this.replace(row, { ...row, redirect: true });
  }

  protected save(): void {
    const rows = this.rows();
    this.saving.set(true);
    this._store.updateUrls({
      redirectUris: rows.filter(r => r.redirect).map(r => r.url),
      allowedCorsOrigins: rows.filter(r => r.cors).map(r => r.url),
      postLogoutRedirectUris: rows.filter(r => r.postLogout).map(r => r.url)
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.reset();
        this._toast.success('URLs saved', 'The client URLs were updated.');
      },
      error: () => this.saving.set(false)
    });
  }

  private replace(row: UrlRow, next: UrlRow): void {
    this.rows.update(rows => rows.map(r => r === row ? next : r));
    this.dirty.set(true);
  }
}
