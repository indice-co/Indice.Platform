import { Component, computed, input, linkedSignal, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideTrash, LucideTriangleAlert } from '@lucide/angular';
import { SearchInputComponent } from 'src/app/ui/list/search-input.component';
import { CaseTypeFormState } from './case-type-form.state';
import { FormText, collectFormTexts, collectLabels } from './form-texts';

type TextFilter = 'all' | 'untranslated' | 'unused';

interface TextRow extends FormText {
  translation: string;
}

/**
 * The form texts translations (`layoutTranslations`): every text of the schema and layout that the server translates, each
 * with its translation. Translations of texts that are no longer in the schema or layout are listed apart, to remove.
 */
@Component({
  selector: 'app-layout-translations',
  imports: [TranslatePipe, SearchInputComponent, LucideTrash, LucideTriangleAlert],
  host: { class: 'block' },
  template: `
    <section>
      <div class="flex flex-wrap items-end justify-between gap-x-6 gap-y-2">
        <div class="min-w-0">
          <h3 class="font-semibold">{{ 'caseTypes.translations.textsHeading' | translate }}</h3>
          <p class="mt-1 max-w-prose text-sm text-base-content/70">{{ 'caseTypes.translations.textsHint' | translate }}</p>
        </div>
        @if (texts().length) {
          <div class="flex items-center gap-3 text-sm">
            <span class="text-base-content/70 tabular-nums">
              {{ 'caseTypes.translations.progress' | translate: { done: translatedCount(), count: texts().length } }}
            </span>
            <progress class="progress progress-primary w-24" [value]="translatedCount()" [max]="texts().length"></progress>
          </div>
        }
      </div>

      @if (labels().length) {
        <div role="note" class="alert alert-warning alert-soft mt-4 items-start text-sm">
          <svg lucideTriangleAlert class="mt-0.5 size-4 shrink-0"></svg>
          <div class="min-w-0">
            <p>{{ 'caseTypes.translations.labelsNotTranslated' | translate: { count: labels().length } }}</p>
            <details class="mt-1">
              <summary class="cursor-pointer text-xs font-medium">{{ 'caseTypes.translations.showLabels' | translate }}</summary>
              <p class="mt-1 text-xs break-words">{{ labels().join(' · ') }}</p>
            </details>
          </div>
        </div>
      }

      @if (schemaMissing()) {
        <p class="mt-4 rounded-box border border-dashed border-base-300 px-4 py-6 text-center text-sm text-base-content/70">
          {{ 'caseTypes.translations.noSchema' | translate }}
        </p>
      } @else {
        <div class="mt-4 flex flex-col gap-2 sm:flex-row sm:items-center">
          <app-search-input class="sm:w-72" [minLength]="1" [value]="search()" [placeholder]="'common.search' | translate" (search)="search.set($event)" />
          <div class="join" role="group" [attr.aria-label]="'caseTypes.translations.filter' | translate">
            @for (option of filters; track option) {
              <button type="button" class="btn btn-sm join-item" [class.btn-active]="filter() === option" [attr.aria-pressed]="filter() === option"
                (click)="filter.set(option)">
                {{ 'caseTypes.translations.filter_' + option | translate }}
                <span class="badge badge-xs badge-ghost tabular-nums">{{ counts()[option] }}</span>
              </button>
            }
          </div>
        </div>

        @if (visible().length) {
          <ul class="mt-3 divide-y divide-base-300 rounded-box border border-base-300">
            @for (row of visible(); track row.text) {
              <li class="grid gap-x-6 gap-y-2 px-4 py-3 sm:grid-cols-2">
                <div class="min-w-0">
                  <p class="text-sm break-words whitespace-pre-wrap">{{ row.text }}</p>
                  <p class="mt-0.5 truncate font-mono text-xs text-base-content/60" [title]="row.paths.join('\\n')">
                    @if (row.paths.length) {
                      {{ 'caseTypes.translations.source_' + row.source | translate }} · {{ row.paths[0] }}
                      @if (row.paths.length > 1) { <span>+{{ row.paths.length - 1 }}</span> }
                    } @else {
                      {{ 'caseTypes.translations.unusedText' | translate }}
                    }
                  </p>
                </div>
                <div class="flex items-start gap-2">
                  @if (row.text.length > 60) {
                    <textarea class="textarea textarea-sm w-full" rows="2" [attr.aria-label]="'caseTypes.translations.translationOf' | translate: { text: row.text }"
                      [value]="row.translation" (input)="translate(row.text, $event)"></textarea>
                  } @else {
                    <input class="input input-sm w-full" type="text" [attr.aria-label]="'caseTypes.translations.translationOf' | translate: { text: row.text }"
                      [value]="row.translation" (input)="translate(row.text, $event)" />
                  }
                  @if (!row.paths.length) {
                    <button type="button" class="btn btn-ghost btn-sm btn-square text-error" [attr.aria-label]="'common.delete' | translate"
                      (click)="state().removeLayoutTranslations([row.text])">
                      <svg lucideTrash class="size-4"></svg>
                    </button>
                  }
                </div>
              </li>
            }
          </ul>
        } @else {
          <p class="mt-3 rounded-box border border-dashed border-base-300 px-4 py-6 text-center text-sm text-base-content/70">
            {{ (texts().length || unused().length ? 'caseTypes.translations.noMatches' : 'caseTypes.translations.noTexts') | translate }}
          </p>
        }

        @if (filter() === 'unused' && unused().length) {
          <div class="mt-3 flex justify-end">
            <button type="button" class="btn btn-ghost btn-sm text-error" (click)="removeUnused()">
              <svg lucideTrash class="size-4"></svg>{{ 'caseTypes.translations.removeUnused' | translate }}
            </button>
          </div>
        }
      }
    </section>
  `
})
export class LayoutTranslationsComponent {
  public readonly state = input.required<CaseTypeFormState>();

  protected readonly filters: TextFilter[] = ['all', 'untranslated', 'unused'];
  protected readonly filter = signal<TextFilter>('all');
  protected readonly search = signal('');

  /** Without a valid schema and layout their texts cannot be listed, and every translation would look unused. */
  protected readonly schemaMissing = computed(() => !this.state().schemaValid() || !this.state().layoutValid());
  protected readonly labels = computed(() => collectLabels(this.state().layout()));
  /** The translatable texts of the schema and layout, with their translations. */
  protected readonly texts = computed<TextRow[]>(() => {
    const translations = this.state().layoutTranslations();
    return collectFormTexts(this.state().schema(), this.state().layout()).map(text => ({ ...text, translation: translations[text.text] ?? '' }));
  });
  /** Translations of texts that the schema and layout no longer have. */
  protected readonly unused = computed<TextRow[]>(() => {
    const texts = new Set(this.texts().map(t => t.text));
    return Object.entries(this.state().layoutTranslations())
      .filter(([text]) => !texts.has(text))
      .map(([text, translation]) => ({ text, translation, source: 'layout' as const, paths: [] }));
  });
  protected readonly translatedCount = computed(() => this.texts().filter(t => !!t.translation.trim()).length);
  protected readonly counts = computed<Record<TextFilter, number>>(() => ({
    all: this.texts().length,
    untranslated: this.texts().length - this.translatedCount(),
    unused: this.unused().length
  }));
  /** Texts translated while the "untranslated" filter is on stay listed until the filter changes, so a row does not vanish mid-typing. */
  private readonly _pinned = linkedSignal<TextFilter, ReadonlySet<string>>({ source: this.filter, computation: () => new Set() });
  protected readonly visible = computed(() => {
    const search = this.search().trim().toLowerCase();
    const pinned = this._pinned();
    const rows = this.filter() === 'unused' ? this.unused()
      : this.filter() === 'untranslated' ? this.texts().filter(t => !t.translation.trim() || pinned.has(t.text))
      : this.texts();
    return search ? rows.filter(r => r.text.toLowerCase().includes(search) || r.translation.toLowerCase().includes(search)) : rows;
  });

  protected translate(text: string, event: Event): void {
    if (this.filter() === 'untranslated') {
      this._pinned.update(pinned => new Set(pinned).add(text));
    }
    this.state().setLayoutTranslation(text, (event.target as HTMLInputElement).value);
  }

  protected removeUnused(): void {
    this.state().removeLayoutTranslations(this.unused().map(t => t.text));
  }
}
