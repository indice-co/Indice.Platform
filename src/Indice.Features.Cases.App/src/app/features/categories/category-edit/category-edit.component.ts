import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { EMPTY, Observable, catchError, finalize } from 'rxjs';
import { LucideSave } from '@lucide/angular';
import { CasesApiService, Category, CategoryRequest, CategoryTranslation } from 'src/app/core/services/cases-api.service';
import { CaseTypeService } from 'src/app/core/services/case-type.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
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
        <section class="border-t border-base-300 p-5 sm:p-6">
          <h3 class="font-semibold">{{ 'categories.translations.heading' | translate }}</h3>
          <p class="mt-1 mb-3 max-w-prose text-sm text-base-content/70">{{ 'categories.translations.hint' | translate }}</p>
          <div class="overflow-x-auto rounded-box border border-base-300">
            <table class="table table-sm">
              <thead>
                <tr>
                  <th class="w-40">{{ 'caseTypes.translations.language' | translate }}</th>
                  <th>{{ 'categories.column.name' | translate }}</th>
                  <th>{{ 'categories.column.description' | translate }}</th>
                </tr>
              </thead>
              <tbody>
                @for (row of translationRows(); track row.code) {
                  <tr>
                    <td>
                      <span class="block font-medium">{{ row.name ?? row.code }}</span>
                      <span class="font-mono text-xs text-base-content/60 uppercase">{{ row.code }}</span>
                    </td>
                    <td class="align-top">
                      <input class="input input-sm w-full min-w-48" type="text" maxlength="128"
                        [attr.aria-label]="('categories.column.name' | translate) + ' · ' + (row.name ?? row.code)"
                        [value]="row.translation.name" (input)="setTranslation(row.code, 'name', value($event))" />
                    </td>
                    <td class="align-top">
                      <input class="input input-sm w-full min-w-48" type="text" maxlength="512"
                        [attr.aria-label]="('categories.column.description' | translate) + ' · ' + (row.name ?? row.code)"
                        [value]="row.translation.description" (input)="setTranslation(row.code, 'description', value($event))" />
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </section>
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
  private readonly _caseTypeService = inject(CaseTypeService);
  private readonly _languages = inject(AppLanguagesService);

  /** Bound from the `:categoryId` route param (edit mode). */
  public readonly categoryId = input<string | undefined>(undefined);

  protected readonly model = signal(new CategoryRequest({ name: '' }));
  /** Name / description per language, keyed by lower case language code. */
  protected readonly translations = signal<Record<string, { name: string; description: string }>>({});
  /** One row per app language, then any other language the category already has translations for. */
  protected readonly translationRows = computed(() => {
    const translations = this.translations();
    const languages = this._languages.languages().map(l => ({ code: l.code.toLowerCase(), name: l.name as string | undefined }));
    const others = Object.keys(translations).filter(code => !languages.some(l => l.code === code)).map(code => ({ code, name: undefined }));
    return [...languages, ...others].map(({ code, name }) => ({ code, name, translation: translations[code] ?? { name: '', description: '' } }));
  });
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
          order: item.order
        }));
        const translations: Record<string, { name: string; description: string }> = {};
        for (const [lang, t] of Object.entries(item.translations ?? {})) {
          translations[lang.toLowerCase()] ??= { name: t?.name ?? '', description: t?.description ?? '' };
        }
        this.translations.set(translations);
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

  protected setTranslation(lang: string, key: 'name' | 'description', value: string): void {
    this.translations.update(translations => {
      const current = translations[lang] ?? { name: '', description: '' };
      return { ...translations, [lang]: { ...current, [key]: value } };
    });
  }

  /** Empty translations are left out: the server would otherwise replace texts with an empty string. */
  private toRequest(): CategoryRequest {
    const translations = Object.fromEntries(Object.entries(this.translations())
      .map(([lang, t]) => [lang, { name: t.name.trim(), description: t.description.trim() }] as const)
      .filter(([, t]) => t.name || t.description)
      .map(([lang, t]) => [lang, new CategoryTranslation({ name: t.name || undefined, description: t.description || undefined })]));
    return new CategoryRequest({ ...this.model(), translations: Object.keys(translations).length ? translations : undefined });
  }

  protected submit(): void {
    if (!this.canSave()) {
      return;
    }
    this.busy.set(true);
    const request = this.toRequest();
    const request$: Observable<Category> = this.isEdit()
      ? this._api.updateCategory(this.categoryId()!, request)
      : this._api.createCategory(request);
    request$.pipe(
      catchError(() => EMPTY),
      finalize(() => this.busy.set(false))
    ).subscribe(() => {
      this._caseTypeService.invalidateCaseTypes();
      this._toaster.show(ToastType.Success, 'toasts.success.title', 'toasts.categorySaved.body');
      this.goBack();
    });
  }

  private goBack(): void {
    this._router.navigate(['/categories']);
  }
}
