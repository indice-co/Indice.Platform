import { Component, computed, input, model, signal } from '@angular/core';
import { LucideLanguages, LucidePlus, LucideX } from '@lucide/angular';
import { ApiScopeTranslation } from 'src/app/core/services/identity-api.service';

export type Translations = { [culture: string]: ApiScopeTranslation };

/**
 * Edits the per-culture display name and description of a scope. The default language values live in the host form;
 * this editor manages the additional cultures (`translations` map keyed by culture code, e.g. `en`, `el`).
 */
@Component({
  selector: 'app-translations-editor',
  imports: [LucideLanguages, LucidePlus, LucideX],
  host: { class: 'block' },
  template: `
    <div class="rounded-box border border-base-300">
      <div class="flex flex-wrap items-center gap-2 border-b border-base-300 px-3 py-2">
        <svg lucideLanguages class="size-4 text-base-content/60"></svg>
        <span class="mr-1 text-sm font-semibold">Translations</span>
        <div role="tablist" class="tabs tabs-box tabs-xs">
          @for (culture of cultures(); track culture) {
            <button type="button" role="tab" class="tab gap-1 font-mono uppercase" [class.tab-active]="culture === current()"
              (click)="selected.set(culture)">
              {{ culture }}
            </button>
          }
        </div>
        @if (!disabled()) {
          <form class="ml-auto flex items-center gap-1" (submit)="$event.preventDefault(); addCulture(code.value); code.value = ''">
            <input #code class="input input-xs w-20 font-mono" maxlength="10" placeholder="e.g. en" aria-label="Culture code" />
            <button type="submit" class="btn btn-ghost btn-xs" aria-label="Add language"><svg lucidePlus class="size-3.5"></svg>Add</button>
          </form>
        }
      </div>
      @if (current(); as culture) {
        <div class="space-y-3 p-3">
          <fieldset class="fieldset py-0">
            <legend class="fieldset-legend">Display name ({{ culture }})</legend>
            <input class="input input-sm w-full" maxlength="200" [disabled]="disabled()" [value]="value()[culture]?.displayName ?? ''"
              (input)="set(culture, 'displayName', $any($event.target).value)" />
          </fieldset>
          <fieldset class="fieldset py-0">
            <legend class="fieldset-legend">Description ({{ culture }})</legend>
            <textarea class="textarea textarea-sm w-full" rows="2" maxlength="1000" [disabled]="disabled()"
              [value]="value()[culture]?.description ?? ''" (input)="set(culture, 'description', $any($event.target).value)"></textarea>
          </fieldset>
          @if (!disabled()) {
            <button type="button" class="btn btn-ghost btn-xs text-error" (click)="removeCulture(culture)">
              <svg lucideX class="size-3.5"></svg>Remove {{ culture }}
            </button>
          }
        </div>
      } @else {
        <p class="px-3 py-4 text-sm text-base-content/60">No translations. Add a culture code to translate the display name and description.</p>
      }
    </div>
  `
})
export class TranslationsEditorComponent {
  public readonly value = model<Translations>({});
  public readonly disabled = input(false);

  protected readonly selected = signal<string | null>(null);
  protected readonly cultures = computed(() => Object.keys(this.value() ?? {}).sort());
  protected readonly current = computed(() => {
    const selected = this.selected();
    const cultures = this.cultures();
    return selected && cultures.includes(selected) ? selected : cultures[0] ?? null;
  });

  protected addCulture(code: string): void {
    const culture = code.trim().toLowerCase();
    if (!culture) {
      return;
    }
    if (!this.value()?.[culture]) {
      this.value.set({ ...this.value(), [culture]: new ApiScopeTranslation({ displayName: '', description: '' }) });
    }
    this.selected.set(culture);
  }

  protected removeCulture(culture: string): void {
    const { [culture]: _, ...rest } = this.value();
    this.value.set(rest);
  }

  protected set(culture: string, field: 'displayName' | 'description', text: string): void {
    const current = this.value()[culture] ?? new ApiScopeTranslation();
    this.value.set({ ...this.value(), [culture]: new ApiScopeTranslation({ ...current, [field]: text }) });
  }
}

/** Normalizes a translations map from the API into `ApiScopeTranslation` instances. */
export function toTranslations(source: { [key: string]: ApiScopeTranslation } | undefined): Translations {
  const result: Translations = {};
  for (const [culture, value] of Object.entries(source ?? {})) {
    result[culture] = new ApiScopeTranslation({ displayName: value?.displayName, description: value?.description });
  }
  return result;
}
