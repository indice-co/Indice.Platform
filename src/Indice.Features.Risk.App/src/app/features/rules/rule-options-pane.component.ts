import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { LucideTriangleAlert } from '@lucide/angular';
import { RiskApiService } from 'src/app/core/services/risk-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { RefreshBus } from 'src/app/ui/list/refresh-bus';
import { SidePaneLayoutComponent, SidePaneService } from 'src/app/ui/side-pane/side-pane';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { RULES_SCOPE } from './rules.component';

type Options = { [key: string]: string };

interface OptionField {
  key: string;
  label: string;
  kind: 'boolean' | 'text' | 'textarea';
  required?: boolean;
  hint?: string;
}

const NAME_KEY = 'name';
const ELIGIBLE_EVENTS_KEY = 'eligibleEvents';
/** Options every rule has (`RuleOptions`), shown first. */
const BASE_FIELDS: OptionField[] = [
  { key: 'friendlyName', label: 'Display name', kind: 'text', required: true },
  { key: 'description', label: 'Description', kind: 'textarea', required: true },
  { key: 'enabled', label: 'Enabled', kind: 'boolean', hint: 'Disabled rules are skipped when risk is evaluated.' }
];

/**
 * Side pane editing a rule's options. The API returns them as a flat string dictionary; rule specific options beyond the
 * base ones are rendered generically (a switch for `True` / `False` values, a text box otherwise).
 */
@Component({
  selector: 'app-rule-options-pane',
  imports: [SidePaneLayoutComponent, EmptyStateComponent, LucideTriangleAlert],
  template: `
    <app-side-pane-layout eyebrow="Rule options" [title]="values()['friendlyName'] || ruleName()" [subtitle]="ruleName()">
      @if (options.isLoading()) {
        <div class="space-y-4">
          @for (row of [1, 2, 3, 4]; track row) { <div class="skeleton h-10 w-full"></div> }
        </div>
      } @else if (!options.hasValue()) {
        <app-empty-state title="Options could not be loaded" message="This rule has no stored options, or the service did not respond.">
          <svg icon lucideTriangleAlert class="size-6"></svg>
          <button actions type="button" class="btn btn-sm" (click)="options.reload()">Try again</button>
        </app-empty-state>
      } @else {
        <form id="rule-options-form" class="space-y-5" (submit)="$event.preventDefault(); save()">
          @for (field of fields(); track field.key) {
            @if (field.kind === 'boolean') {
              <label class="flex items-start justify-between gap-4 rounded-field border border-base-300 px-4 py-3">
                <span>
                  <span class="block text-sm font-medium">{{ field.label }}</span>
                  @if (field.hint) { <span class="block text-xs text-base-content/60">{{ field.hint }}</span> }
                </span>
                <input type="checkbox" class="toggle toggle-success" [checked]="isTrue(values()[field.key])"
                  (change)="set(field.key, $any($event.target).checked ? 'True' : 'False')" />
              </label>
            } @else {
              <fieldset class="fieldset p-0">
                <legend class="fieldset-legend pt-0 text-sm font-medium">
                  {{ field.label }}@if (field.required) { <span class="text-error">*</span> }
                </legend>
                @if (field.kind === 'textarea') {
                  <textarea class="textarea w-full" rows="4" [required]="!!field.required" [value]="values()[field.key] ?? ''"
                    (input)="set(field.key, $any($event.target).value)"></textarea>
                } @else {
                  <input type="text" class="input w-full" [class.font-mono]="field.key !== 'friendlyName'" [required]="!!field.required"
                    [value]="values()[field.key] ?? ''" (input)="set(field.key, $any($event.target).value)" />
                }
                @if (field.hint) { <p class="label text-xs">{{ field.hint }}</p> }
              </fieldset>
            }
          }
        </form>
      }

      <div footer class="flex justify-end gap-2 border-t border-base-300 bg-base-100 px-6 py-4">
        <button type="button" class="btn btn-ghost" (click)="pane.close()">Cancel</button>
        <button type="submit" form="rule-options-form" class="btn btn-primary" [disabled]="!options.hasValue() || saving() || !valid()">
          @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
          Save options
        </button>
      </div>
    </app-side-pane-layout>
  `
})
export class RuleOptionsPaneComponent {
  private readonly _api = inject(RiskApiService);
  private readonly _toasts = inject(ToastService);
  private readonly _refresh = inject(RefreshBus);
  protected readonly pane = inject(SidePaneService);
  /** Route param. */
  public readonly ruleName = input.required<string>();

  protected readonly options = rxResource({
    params: () => this.ruleName(),
    stream: ({ params: ruleName }) => this._api.getRiskRuleOptions(ruleName)
  });
  protected readonly values = signal<Options>({});
  protected readonly saving = signal(false);
  protected readonly fields = computed<OptionField[]>(() => {
    const extra = Object.entries(this.values())
      .filter(([key]) => key !== NAME_KEY && !key.toLowerCase().startsWith(ELIGIBLE_EVENTS_KEY.toLowerCase()) && !BASE_FIELDS.some(f => f.key === key))
      .map(([key, value]): OptionField => ({ key, label: humanize(key), kind: isBoolean(value) ? 'boolean' : 'text' }));
    return [
      ...BASE_FIELDS,
      ...extra,
      { key: ELIGIBLE_EVENTS_KEY, label: 'Eligible events', kind: 'text', hint: 'Event types this rule runs for, comma separated. Leave empty for all.' }
    ];
  });
  protected readonly valid = computed(() => BASE_FIELDS.filter(f => f.required).every(f => !!this.values()[f.key]?.trim()));

  constructor() {
    effect(() => {
      if (this.options.hasValue()) {
        this.values.set(normalize(this.options.value()));
      }
    });
  }

  protected isTrue(value: string | undefined): boolean {
    return value?.toLowerCase() === 'true';
  }

  protected set(key: string, value: string): void {
    this.values.update(values => ({ ...values, [key]: value }));
  }

  protected save(): void {
    if (!this.valid() || this.saving()) {
      return;
    }
    const values = this.values();
    const ruleName = this.ruleName();
    const request: any = {
      ...values,
      // type discriminator of the rule's options
      [NAME_KEY]: `${values[NAME_KEY] || ruleName}RuleOptions`,
      [ELIGIBLE_EVENTS_KEY]: (values[ELIGIBLE_EVENTS_KEY] ?? '').split(',').map(e => e.trim()).filter(Boolean)
    };
    this.saving.set(true);
    this._api.updateRiskRuleOptions(ruleName, request).subscribe({
      next: () => {
        this.saving.set(false);
        this._toasts.success('Options saved', `${values['friendlyName'] || ruleName} has been updated.`);
        this._refresh.notify(RULES_SCOPE);
        this.pane.close();
      },
      // the error toast comes from the problem details interceptor
      error: () => this.saving.set(false)
    });
  }
}

/** Folds indexed `eligibleEvents:0`, `eligibleEvents:1`… entries into the comma separated `eligibleEvents` value. */
function normalize(options: Options): Options {
  const indexed = Object.keys(options).filter(key => key.toLowerCase().startsWith(`${ELIGIBLE_EVENTS_KEY.toLowerCase()}:`));
  const result: Options = Object.fromEntries(Object.entries(options).filter(([key]) => !indexed.includes(key)));
  if (!result[ELIGIBLE_EVENTS_KEY] && indexed.length > 0) {
    result[ELIGIBLE_EVENTS_KEY] = indexed.map(key => options[key]).join(',');
  }
  return result;
}

function isBoolean(value: string | undefined): boolean {
  return value?.toLowerCase() === 'true' || value?.toLowerCase() === 'false';
}

/** `availableBalanceThresholdPercentage` → `Available balance threshold percentage`. */
function humanize(key: string): string {
  const words = key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}
