import { Component, computed, input, OnInit, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon } from '@ng-icons/core';
import { TranslatePipe } from '@ngx-translate/core';
import { CustomCaseAction, SuccessMessage } from 'src/app/core/services/cases-api.service';

export interface CustomActionTrigger {
  redirectToList: boolean | undefined;
  successMessage: SuccessMessage | undefined;
  id: string | undefined;
  value: string | undefined;
}

/**
 * Button classes that workflows set on custom actions (`CustomCaseAction.class`) were written for the previous UI;
 * they map onto DaisyUI buttons here. Anything else is passed through next to `btn`.
 */
const LEGACY_BUTTON_CLASSES: Record<string, string> = {
  'btn-info': 'btn-info',
  'btn-success': 'btn-success',
  'btn-warning': 'btn-warning',
  'btn-error': 'btn-error',
  'btn-delete': 'btn-error',
  'btn-submit': 'btn-primary',
  'btn-submit-form': 'btn-primary',
  'btn-default': 'btn-neutral btn-outline',
  'btn-return': 'btn-neutral btn-outline'
};

/** A workflow-defined action on the case (blocking activity), optionally with a text input. Disabled once triggered. */
@Component({
  selector: 'app-case-custom-action',
  imports: [FormsModule, NgIcon, TranslatePipe],
  template: `
    <h4 class="font-semibold">{{ action().name }}</h4>
    @if (action().description) {
      <p class="mt-1 text-[0.8125rem] text-quiet">{{ action().description }}</p>
      @if (action().hasInput) {
        <input type="text" class="input input-sm mt-2 w-full" [attr.aria-label]="action().description"
               [ngModel]="value()" (ngModelChange)="value.set($event)" />
      }
    }
    <button type="button" class="btn btn-sm mt-3 w-full" [class]="buttonClass()" [title]="'common.execute' | translate"
            [disabled]="triggered()" (click)="trigger()">
      <ng-icon name="lucidePlay" size="0.9rem" /> {{ action().label }}
    </button>
  `,
  host: { class: 'block' }
})
export class CaseCustomActionComponent implements OnInit {
  public readonly action = input.required<CustomCaseAction>();
  public readonly actionTriggered = output<CustomActionTrigger>();
  protected readonly value = signal<string | undefined>(undefined);
  protected readonly triggered = signal(false);
  protected readonly buttonClass = computed(() => (this.action().class ?? 'btn-default')
    .split(/\s+/)
    .map(cls => LEGACY_BUTTON_CLASSES[cls] ?? cls)
    .join(' '));

  public ngOnInit(): void {
    this.value.set(this.action().defaultValue);
  }

  protected trigger(): void {
    this.triggered.set(true);
    this.actionTriggered.emit({
      redirectToList: this.action().redirectToList,
      successMessage: this.action().successMessage,
      id: this.action().id!,
      value: this.value()!
    });
  }
}
