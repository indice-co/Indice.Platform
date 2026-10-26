import { Component, computed, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { CaseTypeFormState, CaseTypeTextTranslation } from './case-type-form.state';
import { LayoutTranslationsComponent } from './layout-translations.component';

interface LanguageRow {
  /** Lower case language code, the key of the translation. */
  code: string;
  name?: string;
  translation: CaseTypeTextTranslation;
  missingTitle: boolean;
}

/** The case type's title and description per language, and the translations of its form texts. */
@Component({
  selector: 'app-case-type-translations',
  imports: [TranslatePipe, LayoutTranslationsComponent],
  host: { class: 'block space-y-8' },
  template: `
    <section>
      <h3 class="font-semibold">{{ 'caseTypes.translations.titleHeading' | translate }}</h3>
      <p class="mt-1 mb-3 max-w-prose text-sm text-base-content/70">{{ 'caseTypes.translations.titleHint' | translate }}</p>
      <div class="overflow-x-auto rounded-box border border-base-300">
        <table class="table table-sm">
          <thead>
            <tr>
              <th class="w-40">{{ 'caseTypes.translations.language' | translate }}</th>
              <th>{{ 'caseTypes.form.titleLabel' | translate }}</th>
              <th>{{ 'caseTypes.form.description' | translate }}</th>
            </tr>
          </thead>
          <tbody>
            <tr class="bg-base-200/50 text-base-content/70">
              <td class="text-xs">{{ 'caseTypes.translations.asEntered' | translate }}</td>
              <td>{{ state().general().title || '—' }}</td>
              <td>{{ state().general().description || '—' }}</td>
            </tr>
            @for (row of rows(); track row.code) {
              <tr>
                <td>
                  <span class="block font-medium">{{ row.name ?? row.code }}</span>
                  <span class="font-mono text-xs text-base-content/60 uppercase">{{ row.code }}</span>
                </td>
                <td class="align-top">
                  <input class="input input-sm w-full min-w-48" type="text" maxlength="128" [class.input-error]="row.missingTitle"
                    [attr.aria-label]="('caseTypes.form.titleLabel' | translate) + ' · ' + (row.name ?? row.code)"
                    [attr.aria-invalid]="row.missingTitle" [value]="row.translation.title"
                    (input)="state().setTranslation(row.code, 'title', value($event))" />
                  @if (row.missingTitle) {
                    <span class="mt-1 block text-xs text-error">{{ 'caseTypes.translations.missingTitle' | translate }}</span>
                  }
                </td>
                <td class="align-top">
                  <input class="input input-sm w-full min-w-48" type="text"
                    [attr.aria-label]="('caseTypes.form.description' | translate) + ' · ' + (row.name ?? row.code)"
                    [value]="row.translation.description" (input)="state().setTranslation(row.code, 'description', value($event))" />
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>

    <app-layout-translations [state]="state()" />
  `
})
export class CaseTypeTranslationsComponent {
  private readonly _languages = inject(AppLanguagesService);
  public readonly state = input.required<CaseTypeFormState>();

  /** One row per app language, then any other language the case type already has translations for. */
  protected readonly rows = computed<LanguageRow[]>(() => {
    const translations = this.state().translations();
    const missingTitle = this.state().translationsMissingTitle();
    const languages = this._languages.languages().map(l => ({ code: l.code.toLowerCase(), name: l.name }));
    const others = Object.keys(translations).filter(code => !languages.some(l => l.code === code)).map(code => ({ code, name: undefined }));
    return [...languages, ...others].map(({ code, name }) => ({
      code,
      name,
      translation: translations[code] ?? { title: '', description: '' },
      missingTitle: missingTitle.includes(code)
    }));
  });

  protected value(event: Event): string {
    return (event.target as HTMLInputElement).value;
  }
}
