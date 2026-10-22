import { Component, computed, input, linkedSignal, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideZap } from '@lucide/angular';
import { CustomCaseAction, SuccessMessage } from 'src/app/core/services/cases-api.service';

export interface CustomActionEvent {
  redirectToList: boolean | undefined;
  successMessage: SuccessMessage | undefined;
  id: string | undefined;
  value: string | undefined;
}

/**
 * Workflow actions may carry a legacy button class (e.g. `btn-info`). These daisyUI button colors are listed
 * here so Tailwind always generates them: btn-primary btn-secondary btn-accent btn-neutral btn-info btn-success
 * btn-warning btn-error.
 */
const DEFAULT_ACTION_CLASS = 'btn-info';

/**
 * A custom workflow action. `compact` renders just the button (for actions without description / input);
 * otherwise a card with the action name, description and optional input.
 */
@Component({
  selector: 'app-case-custom-action',
  imports: [TranslatePipe, LucideZap],
  template: `
    @if (compact()) {
      <button type="button" class="btn" [class]="buttonClass()" [title]="action()?.name ?? ('common.execute' | translate)"
        [disabled]="triggered()" (click)="triggerAction()">
        <svg lucideZap class="size-4"></svg>
        {{ action()?.label }}
      </button>
    } @else {
      <div class="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div class="min-w-0 flex-1 space-y-1">
          <h3 class="font-semibold">{{ action()?.name }}</h3>
          @if (action()?.description) {
            <p class="text-sm text-base-content/70">{{ action()?.description }}</p>
          }
          @if (action()?.hasInput) {
            <input type="text" class="input mt-2 w-full" [value]="value() ?? ''" (input)="value.set($any($event.target).value)"
              [attr.aria-label]="action()?.description ?? action()?.name" />
          }
        </div>
        <button type="button" class="btn shrink-0" [class]="buttonClass()" [title]="'common.execute' | translate"
          [disabled]="triggered()" (click)="triggerAction()">
          <svg lucideZap class="size-4"></svg>
          {{ action()?.label }}
        </button>
      </div>
    }
  `
})
export class CaseCustomActionComponent {
  public readonly action = input<CustomCaseAction | undefined>();
  public readonly compact = input(false);
  public readonly actionTriggered = output<CustomActionEvent>();

  protected readonly value = linkedSignal(() => this.action()?.defaultValue);
  /** Disables the button while the action runs; resets whenever the parent reloads the case actions. */
  protected readonly triggered = linkedSignal({ source: this.action, computation: () => false });
  protected readonly buttonClass = computed(() => this.action()?.class || DEFAULT_ACTION_CLASS);

  protected triggerAction(): void {
    this.triggered.set(true);
    const action = this.action();
    this.actionTriggered.emit({
      redirectToList: action?.redirectToList,
      successMessage: action?.successMessage,
      id: action?.id,
      value: this.value()
    });
  }
}
