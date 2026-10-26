import { Component, effect, inject, input, output, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiScopeInfo, UpdateApiScopeRequest } from 'src/app/core/services/identity-api.service';
import { Translations, TranslationsEditorComponent, toTranslations } from './translations-editor.component';

/** Details form of an API scope (display name, description, consent options and translations). */
@Component({
  selector: 'app-scope-form',
  imports: [ReactiveFormsModule, TranslationsEditorComponent],
  host: { class: 'block' },
  template: `
    <form class="space-y-5" [formGroup]="form" (ngSubmit)="submit()">
      <div class="grid gap-4 sm:grid-cols-2">
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Name</legend>
          <input class="input w-full font-mono" [value]="scope().name" disabled />
        </fieldset>
        <fieldset class="fieldset">
          <legend class="fieldset-legend">Display name</legend>
          <input class="input w-full" formControlName="displayName" maxlength="200" />
        </fieldset>
      </div>
      <fieldset class="fieldset">
        <legend class="fieldset-legend">Description</legend>
        <textarea class="textarea w-full" rows="2" formControlName="description" maxlength="1000"></textarea>
      </fieldset>

      <div class="grid gap-3 sm:grid-cols-2">
        <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5">
          <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" formControlName="showInDiscoveryDocument" />
          <span>
            <span class="block text-sm font-semibold">Show in discovery document</span>
            <span class="block text-xs text-base-content/65">Listed in the OpenID Connect discovery document.</span>
          </span>
        </label>
        <label class="flex cursor-pointer items-start gap-3 rounded-field border border-base-300 p-3 has-checked:border-primary/50 has-checked:bg-primary/5">
          <input type="checkbox" class="toggle toggle-sm toggle-primary mt-0.5" formControlName="emphasize" />
          <span>
            <span class="block text-sm font-semibold">Emphasize</span>
            <span class="block text-xs text-base-content/65">Highlighted on the consent screen.</span>
          </span>
        </label>
      </div>

      <app-translations-editor [value]="translations()" (valueChange)="onTranslations($event)" [disabled]="readonly()" />

      @if (!readonly()) {
        <div class="flex justify-end gap-2">
          <button type="button" class="btn btn-ghost btn-sm" [disabled]="pristine() || saving()" (click)="reset()">Discard</button>
          <button type="submit" class="btn btn-primary btn-sm" [disabled]="form.invalid || pristine() || saving()">
            @if (saving()) { <span class="loading loading-spinner loading-sm"></span> }
            Save scope
          </button>
        </div>
      }
    </form>
  `
})
export class ScopeFormComponent {
  public readonly scope = input.required<ApiScopeInfo>();
  public readonly readonly = input(false);
  public readonly saving = input(false);
  public readonly save = output<UpdateApiScopeRequest>();

  protected readonly translations = signal<Translations>({});
  private readonly _translationsDirty = signal(false);
  protected readonly form = inject(NonNullableFormBuilder).group({
    displayName: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(1000)],
    showInDiscoveryDocument: [false],
    emphasize: [false]
  });
  private readonly _formDirty = signal(false);

  constructor() {
    effect(() => {
      this.scope();
      this.reset();
    });
    effect(() => (this.readonly() ? this.form.disable() : this.form.enable()));
    this.form.valueChanges.subscribe(() => this._formDirty.set(this.form.dirty));
  }

  protected pristine(): boolean {
    return !this._formDirty() && !this._translationsDirty();
  }

  protected onTranslations(value: Translations): void {
    this.translations.set(value);
    this._translationsDirty.set(true);
  }

  protected reset(): void {
    const scope = this.scope();
    this.form.reset({
      displayName: scope.displayName ?? '',
      description: scope.description ?? '',
      showInDiscoveryDocument: !!scope.showInDiscoveryDocument,
      emphasize: !!scope.emphasize
    });
    this.translations.set(toTranslations(scope.translations));
    this._formDirty.set(false);
    this._translationsDirty.set(false);
  }

  /** Marks the form clean after the host saved it. */
  public markSaved(): void {
    this.form.markAsPristine();
    this._formDirty.set(false);
    this._translationsDirty.set(false);
  }

  protected submit(): void {
    const value = this.form.getRawValue();
    this.save.emit(new UpdateApiScopeRequest({
      displayName: value.displayName || undefined,
      description: value.description || undefined,
      showInDiscoveryDocument: value.showInDiscoveryDocument,
      emphasize: value.emphasize,
      translations: this.translations()
    }));
  }
}
