import { Component, OnInit, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { filter, switchMap } from 'rxjs';
import { LucideTrash } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { CreateRoleRequest, IdentityApiService, UpdateRoleRequest } from 'src/app/core/services/identity-api.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';

/** Side pane form that creates a role, or shows / edits the one in the `id` route param. */
@Component({
  selector: 'app-role-edit',
  imports: [ReactiveFormsModule, SidePaneLayoutComponent, LucideTrash],
  template: `
    <app-side-pane-layout [title]="id() ? (name() || 'Role') : 'New role'"
      [subtitle]="id() ? 'Users holding this role are authorized by it in every application.' : 'Group users that share the same access.'">
      @if (loading()) {
        <div class="space-y-4">
          @for (row of [1, 2]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else {
        <form id="role-form" class="space-y-4" [formGroup]="form" (ngSubmit)="submit()">
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Name{{ id() ? '' : ' *' }}</legend>
            <input class="input w-full" formControlName="name" maxlength="64" placeholder="e.g. Approver"
              [class.input-error]="form.controls.name.invalid && form.controls.name.touched" />
            @if (form.controls.name.touched && form.controls.name.hasError('required')) {
              <p class="label text-error">A name is required.</p>
            } @else if (id()) {
              <p class="label whitespace-normal">The name of a role cannot change once created.</p>
            }
          </fieldset>
          <fieldset class="fieldset">
            <legend class="fieldset-legend">Description</legend>
            <textarea class="textarea w-full" rows="4" formControlName="description" maxlength="512"
              placeholder="What does this role grant?"></textarea>
          </fieldset>
        </form>
      }
      <div footer class="flex items-center gap-2 border-t border-base-300 px-6 py-4">
        @if (id() && canEdit) {
          <button type="button" class="btn btn-ghost text-error" (click)="remove()" [disabled]="saving()">
            <svg lucideTrash class="size-4"></svg>Delete
          </button>
        }
        <span class="flex-1"></span>
        <button type="button" class="btn btn-ghost" (click)="pane.close()">{{ canEdit ? 'Cancel' : 'Close' }}</button>
        @if (canEdit) {
          <button type="submit" form="role-form" class="btn btn-primary" [disabled]="form.invalid || saving() || loading()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            {{ id() ? 'Save' : 'Create role' }}
          </button>
        }
      </div>
    </app-side-pane-layout>
  `
})
export class RoleEditComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _dialog = inject(DialogService);
  private readonly _toast = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);
  protected readonly canEdit = inject(AuthService).isAdminUIUsersWriter();

  /** Bound from the `role/:id` pane route; absent when creating. */
  public readonly id = input<string>();

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly name = signal('');
  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(64)]],
    description: ['', Validators.maxLength(512)]
  });

  public ngOnInit(): void {
    const id = this.id();
    if (!this.canEdit) {
      this.form.disable();
    }
    if (!id) {
      return;
    }
    this.form.controls.name.disable();
    this.loading.set(true);
    this._api.getRole(id).subscribe({
      next: role => {
        this.name.set(role.name ?? '');
        this.form.setValue({ name: role.name ?? '', description: role.description ?? '' });
        this.loading.set(false);
      },
      error: () => this.pane.close()
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    const id = this.id();
    const request$ = id
      ? this._api.updateRole(id, new UpdateRoleRequest({ description: value.description || undefined }))
      : this._api.createRole(new CreateRoleRequest({ name: value.name.trim(), description: value.description || undefined }));
    this.saving.set(true);
    request$.subscribe({
      next: role => {
        this._toast.success(id ? 'Role updated' : 'Role created', `Role '${role.name}' was ${id ? 'updated' : 'created'}.`);
        this._refresh.notify('roles');
        this.pane.close();
      },
      error: () => this.saving.set(false)
    });
  }

  protected remove(): void {
    const id = this.id()!;
    this._dialog
      .confirm({
        title: 'Delete role?',
        message: `Role '${this.name()}' will be removed from every user that holds it. This cannot be undone.`,
        confirmLabel: 'Delete',
        tone: 'danger'
      })
      .pipe(filter(Boolean), switchMap(() => this._api.deleteRole(id)))
      .subscribe(() => {
        this._toast.success('Role deleted', `Role '${this.name()}' was deleted.`);
        this._refresh.notify('roles');
        this.pane.close();
      });
  }
}
