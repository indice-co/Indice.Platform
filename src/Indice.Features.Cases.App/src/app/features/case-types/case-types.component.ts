import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, catchError, filter, switchMap, take } from 'rxjs';
import { LucideCheck, LucideMinus, LucidePencil, LucidePlus, LucideShapes, LucideTrash } from '@lucide/angular';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { CasesApiService, CaseTypePartial } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { DialogService } from 'src/app/ui/dialog/dialog.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

@Component({
  selector: 'app-case-types',
  imports: [
    RouterLink, TranslatePipe, PageHeaderComponent, EmptyStateComponent,
    LucideCheck, LucideMinus, LucidePencil, LucidePlus, LucideShapes, LucideTrash
  ],
  template: `
    <app-page-header [title]="'caseTypes.title' | translate" [subtitle]="'caseTypes.subtitle' | translate">
      <div actions>
        <a routerLink="/case-types/create" class="btn btn-primary btn-sm">
          <svg lucidePlus class="size-4"></svg>{{ 'caseTypes.create.title' | translate }}
        </a>
      </div>
    </app-page-header>

    <div class="panel overflow-x-auto">
      <table class="table table-sm">
        <thead>
          <tr>
            <th>{{ 'caseTypes.column.title' | translate }}</th>
            <th>{{ 'caseTypes.column.code' | translate }}</th>
            <th>{{ 'caseTypes.column.tags' | translate }}</th>
            <th class="text-center">{{ 'caseTypes.column.menuItem' | translate }}</th>
            <th class="w-24 text-right"><span class="sr-only">{{ 'caseTypes.column.actions' | translate }}</span></th>
          </tr>
        </thead>
        <tbody>
          @if (loading()) {
            @for (row of [1, 2, 3, 4]; track row) {
              <tr>
                <td><div class="skeleton h-4 w-40"></div></td>
                <td><div class="skeleton h-4 w-24"></div></td>
                <td><div class="skeleton h-4 w-20"></div></td>
                <td></td>
                <td></td>
              </tr>
            }
          } @else {
            @for (item of items(); track item.id) {
              <tr class="hover:bg-base-200/60">
                <td>
                  <a class="font-medium hover:text-primary" [routerLink]="['/case-types', item.id, 'edit']">{{ item.title }}</a>
                  @if (item.description) {
                    <p class="max-w-md truncate text-xs text-base-content/60">{{ item.description }}</p>
                  }
                </td>
                <td><span class="font-mono text-xs">{{ item.code }}</span></td>
                <td>
                  <div class="flex flex-wrap gap-1">
                    @for (tag of tags(item); track tag) {
                      <span class="badge badge-ghost badge-sm">{{ tag }}</span>
                    }
                  </div>
                </td>
                <td class="text-center">
                  @if (item.isMenuItem) {
                    <svg lucideCheck class="inline size-4 text-success" [attr.aria-label]="'common.yes' | translate"></svg>
                  } @else {
                    <svg lucideMinus class="inline size-4 text-base-content/30" [attr.aria-label]="'common.no' | translate"></svg>
                  }
                </td>
                <td class="text-right whitespace-nowrap">
                  <span class="tooltip" [attr.data-tip]="'common.edit' | translate">
                    <a class="btn btn-ghost btn-xs btn-square" [routerLink]="['/case-types', item.id, 'edit']" [attr.aria-label]="'common.edit' | translate">
                      <svg lucidePencil class="size-4"></svg>
                    </a>
                  </span>
                  <span class="tooltip tooltip-left" [attr.data-tip]="'common.delete' | translate">
                    <button type="button" class="btn btn-ghost btn-xs btn-square text-error" (click)="delete(item)" [attr.aria-label]="'common.delete' | translate">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  </span>
                </td>
              </tr>
            } @empty {
              <tr>
                <td colspan="5">
                  <app-empty-state [title]="'caseTypes.empty.title' | translate" [message]="'caseTypes.empty.message' | translate">
                    <svg icon lucideShapes class="size-5"></svg>
                    <a actions routerLink="/case-types/create" class="btn btn-primary btn-sm">{{ 'caseTypes.create.title' | translate }}</a>
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
export class CaseTypesComponent implements OnInit {
  private readonly _api = inject(CasesApiService);
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _dialog = inject(DialogService);
  private readonly _toaster = inject(TranslatedToasterService);

  protected readonly items = signal<CaseTypePartial[]>([]);
  protected readonly loading = signal(true);

  public ngOnInit(): void {
    this.load();
  }

  protected tags(item: CaseTypePartial): string[] {
    return (item.tags ?? '').split(',').map(t => t.trim()).filter(Boolean);
  }

  protected delete(item: CaseTypePartial): void {
    this._dialog.confirm({
      title: 'caseTypes.delete.title',
      message: 'caseTypes.delete.confirm',
      confirmLabel: 'common.delete',
      tone: 'danger'
    }).pipe(
      filter(Boolean),
      switchMap(() => this._api.deleteCaseType(item.id!)),
      catchError(err => {
        this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title);
        return EMPTY;
      })
    ).subscribe(() => {
      this._caseTypeService.invalidateCaseTypes();
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.caseTypeDeleted.body');
      this.load();
    });
  }

  private load(): void {
    this.loading.set(true);
    this._caseTypeService.getCaseTypes().pipe(take(1)).subscribe({
      next: result => {
        this.items.set(result.items ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
