import { Component, computed, input, model } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { LucideLanguages } from '@lucide/angular';
import { TranslatePipe } from '@ngx-translate/core';

/**
 * Text field with a culture switcher. The first culture edits `control` (the value stored on the entity itself);
 * any other culture edits `translations[culture]`. The selected culture is a two-way `culture` model so that
 * sibling fields can switch together.
 */
@Component({
  selector: 'app-translatable-field',
  imports: [ReactiveFormsModule, TranslatePipe, LucideLanguages],
  template: `
    <!-- only the button is a join-item: marking the dropdown wrapper as one too draws a second (currentColor) border around it -->
    <div class="join w-full">
      <div class="dropdown flex">
        <button type="button" tabindex="0" class="btn join-item gap-1 px-3 font-mono text-xs" [attr.aria-label]="'Clients.TranslatableField.Language' | translate: { culture: culture() }"
          [class.h-auto]="multiline()" [class]="isPrimary() ? 'border-base-content/20' : 'btn-primary btn-soft'">
          <svg lucideLanguages class="size-3.5"></svg>{{ culture() }}
        </button>
        <ul tabindex="0" class="dropdown-content menu z-30 mt-1 w-36 rounded-box border border-base-300 bg-base-100 p-1 shadow-lg">
          @for (item of cultures(); track item; let first = $first) {
            <li>
              <button type="button" [class.menu-active]="item === culture()" (click)="culture.set(item); blur()">
                <span class="font-mono text-xs">{{ item }}</span>
                @if (first) { <span class="text-xs text-base-content/60">{{ 'Clients.TranslatableField.Default' | translate }}</span> }
              </button>
            </li>
          }
        </ul>
      </div>
      @if (isPrimary()) {
        @if (multiline()) {
          <textarea class="textarea join-item w-full" rows="3" [formControl]="control()" [attr.maxlength]="maxlength()" [placeholder]="placeholder()"
            [class.textarea-error]="invalid()"></textarea>
        } @else {
          <input class="input join-item w-full" [formControl]="control()" [attr.maxlength]="maxlength()" [placeholder]="placeholder()"
            [class.input-error]="invalid()" />
        }
      } @else {
        @if (multiline()) {
          <textarea class="textarea join-item w-full" rows="3" [value]="translations()[culture()] ?? ''" [attr.maxlength]="maxlength()"
            [placeholder]="control().value || placeholder()" [disabled]="control().disabled" (input)="setTranslation($any($event.target).value)"></textarea>
        } @else {
          <input class="input join-item w-full" [value]="translations()[culture()] ?? ''" [attr.maxlength]="maxlength()"
            [placeholder]="control().value || placeholder()" [disabled]="control().disabled" (input)="setTranslation($any($event.target).value)" />
        }
      }
    </div>
  `
})
export class TranslatableFieldComponent {
  public readonly control = input.required<FormControl<string>>();
  public readonly cultures = input.required<string[]>();
  public readonly culture = model.required<string>();
  /** Values per non-default culture. */
  public readonly translations = model<Record<string, string>>({});
  public readonly multiline = input(false);
  public readonly maxlength = input<number>();
  public readonly placeholder = input('');
  public readonly invalid = input(false);

  protected readonly isPrimary = computed(() => this.culture() === this.cultures()[0]);

  protected setTranslation(value: string): void {
    this.translations.update(map => ({ ...map, [this.culture()]: value }));
  }

  protected blur(): void {
    (document.activeElement as HTMLElement | null)?.blur();
  }
}
