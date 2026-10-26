import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { LucidePalette, LucideRotateCcw } from '@lucide/angular';
import { AuthService } from 'src/app/core/services/auth.service';
import { ClientThemeConfigRequest, IdentityApiService } from 'src/app/core/services/identity-api.service';
import { EmptyStateComponent } from 'src/app/ui/list/empty-state.component';
import { ToastService } from 'src/app/ui/toast/toast.service';
import { ClientStore } from './client-store.service';

type FieldKind = 'color' | 'text' | 'url' | 'boolean' | 'number' | 'enum';

interface ThemeField {
  key: string;
  label: string;
  description?: string;
  kind: FieldKind;
  options?: string[];
}

interface JsonSchemaProperty {
  type?: string | string[];
  title?: string;
  description?: string;
  format?: string;
  enum?: unknown[];
}

const HEX = /^#(?:[\da-f]{3}|[\da-f]{6})$/i;

/** Splits `primaryColor` into `Primary color`. */
function humanize(key: string): string {
  const words = key.replace(/([a-z\d])([A-Z])/g, '$1 $2').replace(/[_-]+/g, ' ').toLowerCase();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

/** Turns the flat JSON schema of the server's theme configuration type into form fields. */
function toFields(schema: unknown): ThemeField[] {
  const properties = (schema as { properties?: Record<string, JsonSchemaProperty> } | null)?.properties ?? {};
  return Object.entries(properties).map(([key, property]) => {
    const types = Array.isArray(property.type) ? property.type : [property.type];
    const type = types.find(t => t && t !== 'null') ?? 'string';
    let kind: FieldKind = 'text';
    if (property.enum?.length) {
      kind = 'enum';
    } else if (type === 'boolean') {
      kind = 'boolean';
    } else if (type === 'number' || type === 'integer') {
      kind = 'number';
    } else if (property.format === 'color' || /colou?r/i.test(key)) {
      kind = 'color';
    } else if (property.format === 'uri' || /(image|url|uri|logo)$/i.test(key)) {
      kind = 'url';
    }
    return {
      key,
      label: property.title || humanize(key),
      description: property.description,
      kind,
      options: property.enum?.filter(v => v != null).map(String)
    };
  });
}

/** Sign in page theme of the client, edited through a form generated from the server's theme schema. */
@Component({
  selector: 'app-client-theme',
  imports: [EmptyStateComponent, LucidePalette, LucideRotateCcw],
  template: `
    @if (loading()) {
      <div class="grid gap-6 lg:grid-cols-2"><div class="skeleton h-80"></div><div class="skeleton h-80"></div></div>
    } @else if (fields().length === 0) {
      <div class="panel">
        <app-empty-state title="No theme options" message="The identity server does not expose any theme settings for clients.">
          <svg icon lucidePalette class="size-5"></svg>
        </app-empty-state>
      </div>
    } @else {
      <div class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,24rem)]">
        <form class="panel space-y-4 p-6" (submit)="$event.preventDefault(); save()">
          <div>
            <h2 class="font-semibold">Sign in page theme</h2>
            <p class="text-sm text-base-content/60">Colors and imagery used when users sign in through this client.</p>
          </div>
          @for (field of fields(); track field.key) {
            @switch (field.kind) {
              @case ('boolean') {
                <label class="flex cursor-pointer items-start gap-3">
                  <input type="checkbox" class="toggle toggle-primary toggle-sm mt-0.5" [checked]="!!values()[field.key]" [disabled]="!canEdit"
                    (change)="set(field.key, $any($event.target).checked)" />
                  <span>
                    <span class="block text-sm font-medium">{{ field.label }}</span>
                    @if (field.description) { <span class="block text-xs text-base-content/60">{{ field.description }}</span> }
                  </span>
                </label>
              }
              @default {
                <fieldset class="fieldset">
                  <legend class="fieldset-legend">{{ field.label }}</legend>
                  @switch (field.kind) {
                    @case ('color') {
                      <div class="join w-full max-w-xs">
                        <input type="color" class="join-item h-10 w-12 cursor-pointer border border-base-300 bg-base-100 p-1" [disabled]="!canEdit"
                          [value]="colorValue(field.key)" (input)="set(field.key, $any($event.target).value)" [attr.aria-label]="field.label + ' picker'" />
                        <input class="input join-item w-full font-mono text-sm" [value]="values()[field.key] ?? ''" placeholder="#000000" [disabled]="!canEdit"
                          [class.input-error]="invalidColor(field.key)" (input)="set(field.key, $any($event.target).value)" />
                        <button type="button" class="btn join-item" title="Clear" [disabled]="!canEdit" (click)="set(field.key, '')">
                          <svg lucideRotateCcw class="size-4"></svg>
                        </button>
                      </div>
                      @if (invalidColor(field.key)) { <p class="label text-error">Use a hex color such as #3d5afe.</p> }
                    }
                    @case ('number') {
                      <input type="number" class="input w-full max-w-xs" [value]="values()[field.key] ?? ''" [disabled]="!canEdit"
                        (input)="set(field.key, $any($event.target).value === '' ? null : +$any($event.target).value)" />
                    }
                    @case ('enum') {
                      <select class="select w-full max-w-xs" [disabled]="!canEdit" (change)="set(field.key, $any($event.target).value || null)">
                        <option value="" [selected]="!values()[field.key]">Default</option>
                        @for (option of field.options; track option) {
                          <option [value]="option" [selected]="values()[field.key] === option">{{ option }}</option>
                        }
                      </select>
                    }
                    @default {
                      <input class="input w-full" [class.font-mono]="field.kind === 'url'" [class.text-sm]="field.kind === 'url'" [value]="values()[field.key] ?? ''"
                        [disabled]="!canEdit" [placeholder]="field.kind === 'url' ? 'https://…' : ''" (input)="set(field.key, $any($event.target).value)" />
                    }
                  }
                  @if (field.description) { <p class="label whitespace-normal">{{ field.description }}</p> }
                </fieldset>
              }
            }
          }
          @if (canEdit) {
            <div class="flex justify-end pt-2">
              <button type="submit" class="btn btn-primary" [disabled]="saving() || hasErrors()">
                @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
                Save theme
              </button>
            </div>
          }
        </form>

        <section class="panel h-fit overflow-hidden" aria-label="Preview">
          <p class="border-b border-base-300 px-4 py-2 text-xs font-semibold tracking-wider text-base-content/60 uppercase">Preview</p>
          <div class="grid min-h-72 place-items-center bg-base-200 bg-cover bg-center p-6" [style.background-image]="backgroundImage()"
            [style.background-color]="preview().accent">
            <div class="w-full max-w-60 space-y-3 rounded-box bg-base-100 p-5 shadow-lg">
              <div class="h-3 w-24 rounded-full" [style.background]="preview().primary"></div>
              <div class="h-8 rounded-field border border-base-300"></div>
              <div class="h-8 rounded-field border border-base-300"></div>
              <div class="h-9 rounded-field" [style.background]="preview().primary"></div>
              <div class="mx-auto h-2 w-20 rounded-full" [style.background]="preview().secondary"></div>
            </div>
          </div>
        </section>
      </div>
    }
  `
})
export class ClientThemeComponent implements OnInit {
  private readonly _api = inject(IdentityApiService);
  private readonly _store = inject(ClientStore);
  private readonly _toast = inject(ToastService);
  protected readonly canEdit = inject(AuthService).isAdminUIClientsWriter();

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly fields = signal<ThemeField[]>([]);
  protected readonly values = signal<Record<string, any>>({});
  protected readonly hasErrors = computed(() => this.fields().some(f => f.kind === 'color' && this.invalidColor(f.key)));
  protected readonly preview = computed(() => {
    const values = this.values();
    const color = (pattern: RegExp, fallback: string) => {
      const key = this.fields().find(f => f.kind === 'color' && pattern.test(f.key))?.key;
      return key && HEX.test(values[key] ?? '') ? values[key] : fallback;
    };
    return {
      primary: color(/primary/i, 'var(--color-primary)'),
      secondary: color(/secondary/i, 'var(--color-secondary)'),
      accent: color(/accent|background/i, 'var(--color-base-200)')
    };
  });
  protected readonly backgroundImage = computed(() => {
    const key = this.fields().find(f => f.kind === 'url' && /image/i.test(f.key))?.key;
    const url = key ? this.values()[key] : '';
    return url ? `url("${String(url).replace(/"/g, '%22')}")` : null;
  });

  public ngOnInit(): void {
    this._api.getClientTheme(this._store.clientId()).subscribe({
      next: response => {
        this.fields.set(toFields(response.schema));
        this.values.set(response.data ? response.data.toJSON() : {});
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  protected set(key: string, value: unknown): void {
    this.values.update(values => ({ ...values, [key]: value }));
  }

  protected colorValue(key: string): string {
    const value = this.values()[key];
    if (!HEX.test(value ?? '')) {
      return '#000000';
    }
    return value.length === 4 ? '#' + [...value.slice(1)].map((c: string) => c + c).join('') : value;
  }

  protected invalidColor(key: string): boolean {
    const value = this.values()[key];
    return !!value && !HEX.test(value);
  }

  protected save(): void {
    const body: Record<string, unknown> = {};
    for (const field of this.fields()) {
      const value = this.values()[field.key];
      body[field.key] = value === '' ? null : value ?? null;
    }
    this.saving.set(true);
    // a plain object, so that theme properties beyond the default ones are sent too
    this._api.createOrUpdateClientTheme(this._store.clientId(), body as unknown as ClientThemeConfigRequest).subscribe({
      next: () => {
        this.saving.set(false);
        this._toast.success('Theme saved', 'The sign in page theme of the client was updated.');
      },
      error: () => this.saving.set(false)
    });
  }
}
