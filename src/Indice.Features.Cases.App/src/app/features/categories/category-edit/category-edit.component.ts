import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable, catchError, finalize } from 'rxjs';
import { LucideSave } from '@lucide/angular';
import { CasesApiService, Category, CategoryRequest } from 'src/app/core/services/cases-api.service';
import { ToastType, TranslatedToasterService } from 'src/app/shared/services/translated-toaster.service';
import { PageHeaderComponent } from 'src/app/ui/page-header/page-header.component';

/** Create (no `:categoryId`) or edit a case type category. */
@Component({
  selector: 'app-category-edit',
  imports: [RouterLink, TranslatePipe, PageHeaderComponent, LucideSave],
  template: `
    <app-page-header [title]="(isEdit() ? 'categories.edit.title' : 'categories.create.title') | translate" />

    @if (loading()) {
      <div class="panel max-w-3xl space-y-4 p-6">
        <div class="skeleton h-10 w-full"></div>
        <div class="skeleton h-24 w-full"></div>
      </div>
    } @else {
      <form class="panel max-w-3xl" novalidate (submit)="$event.preventDefault(); submit()">
        <fieldset class="fieldset grid gap-x-6 gap-y-3 p-5 sm:p-6 md:grid-cols-2">
          <label>
            <span class="label mb-1">{{ 'categories.column.name' | translate }} <span class="text-error">*</span></span>
            <input class="input w-full" type="text" required maxlength="128" [value]="model().name ?? ''" (input)="patch({ name: value($event) })" />
          </label>
          <label>
            <span class="label mb-1">{{ 'categories.column.order' | translate }}</span>
            <input class="input w-full" type="number" [value]="model().order ?? ''" (input)="patch({ order: numberValue($event) })" />
          </label>
          <label class="md:col-span-2">
            <span class="label mb-1">{{ 'categories.column.description' | translate }}</span>
            <textarea class="textarea w-full" rows="3" maxlength="512" [value]="model().description ?? ''" (input)="patch({ description: value($event) })"></textarea>
          </label>
        </fieldset>
        <div class="flex justify-end gap-2 border-t border-base-300 px-5 py-3 sm:px-6">
          <a class="btn btn-ghost" routerLink="/categories">{{ 'common.cancel' | translate }}</a>
          <button type="submit" class="btn btn-primary" [disabled]="!canSave()">
            @if (busy()) { <span class="loading loading-spinner loading-sm"></span> } @else { <svg lucideSave class="size-4"></svg> }
            {{ 'common.save' | translate }}
          </button>
        </div>
      </form>
    }
  `
})
export class CategoryEditComponent implements OnInit {
  private readonly _router = inject(Router);
  private readonly _api = inject(CasesApiService);
  private readonly _toaster = inject(TranslatedToasterService);

  /** Bound from the `:categoryId` route param (edit mode). */
  public readonly categoryId = input<string | undefined>(undefined);

  protected readonly model = signal(new CategoryRequest({ name: '' }));
  protected readonly busy = signal(false);
  protected readonly loading = signal(false);
  protected readonly isEdit = computed(() => !!this.categoryId());
  protected readonly canSave = computed(() => !this.busy() && !!this.model().name?.trim());

  public ngOnInit(): void {
    const id = this.categoryId();
    if (id) {
      this.loading.set(true);
      this._api.getCategoryById(id).pipe(
        catchError(err => {
          this._toaster.show(ToastType.Error, 'toasts.error.title', err?.detail ?? err?.title ?? err?.message);
          this.goBack();
          return EMPTY;
        }),
        finalize(() => this.loading.set(false))
      ).subscribe((item: Category) => {
        this.model.set(new CategoryRequest({
          name: item.name,
          description: item.description,
          order: item.order,
          translations: item.translations
        }));
      });
    }
  }

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }

  protected numberValue(event: Event): number | undefined {
    const raw = (event.target as HTMLInputElement).value;
    return raw === '' ? undefined : Number(raw);
  }

  protected patch(changes: Partial<CategoryRequest>): void {
    this.model.update(model => new CategoryRequest({ ...model, ...changes }));
  }

  protected submit(): void {
    if (!this.canSave()) {
      return;
    }
    this.busy.set(true);
    const request$: Observable<Category> = this.isEdit()
      ? this._api.updateCategory(this.categoryId()!, this.model())
      : this._api.createCategory(this.model());
    request$.pipe(
      catchError(() => EMPTY),
      finalize(() => this.busy.set(false))
    ).subscribe(() => {
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.categorySaved.body');
      this.goBack();
    });
  }

  private goBack(): void {
    this._router.navigate(['/categories']);
  }
}
