import {
  Component, Directive, ElementRef, Injector, Signal, TemplateRef, afterNextRender, computed, contentChild, contentChildren, inject, input,
  linkedSignal, model, output, viewChild, viewChildren
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LucideCheck } from '@lucide/angular';

let nextStepId = 0;

/** Context of the `appStepLabel` template. */
export interface StepLabelContext {
  $implicit: StepComponent;
}

/** Custom step header: `<ng-template appStepLabel let-step>…</ng-template>` inside an `<app-step>`. */
@Directive({ selector: 'ng-template[appStepLabel]' })
export class StepLabelDirective {
  public readonly template = inject<TemplateRef<StepLabelContext>>(TemplateRef);

  public static ngTemplateContextGuard(_dir: StepLabelDirective, ctx: unknown): ctx is StepLabelContext {
    return true;
  }
}

/**
 * Lazy step body: `<ng-template appStepContent>…</ng-template>` inside an `<app-step>` is only instantiated once the step
 * is first opened. Plain projected content is created up front, like any projected content.
 */
@Directive({ selector: 'ng-template[appStepContent]' })
export class StepContentDirective {
  public readonly template = inject<TemplateRef<unknown>>(TemplateRef);
}

export type StepState = 'active' | 'done' | 'error' | 'pending';

/** A single step of an `<app-stepper>`. Renders nothing by itself; the stepper renders its header and body. */
@Component({
  selector: 'app-step',
  template: `<ng-template><ng-content /></ng-template>`
})
export class StepComponent {
  private readonly _stepper = inject(StepperComponent, { optional: true });
  private readonly _autoId = `app-step-${nextStepId++}`;
  private readonly _projected = viewChild.required(TemplateRef);
  private readonly _lazy = contentChild(StepContentDirective);

  /** Header text, used when no `appStepLabel` template is given. */
  public readonly label = input<string>();
  /** Secondary header text, shown under the label on wide screens. */
  public readonly hint = input<string>();
  /** Unique id of the step; also lets the stepper select it by id. Auto-generated when omitted. */
  public readonly id = input<string>();
  /** The step is done; in a linear stepper the steps after it are reachable only once it is. */
  public readonly completed = input(false);
  /** The step has errors; shown on its header once the step has been opened and left. */
  public readonly invalid = input(false);
  /** Whether the step can be reopened from its header. Set to false for steps behind a point of no return. */
  public readonly editable = input(true);

  public readonly labelTemplate = contentChild(StepLabelDirective);
  public readonly stepId = computed(() => this.id() || this._autoId);
  public readonly content = computed<TemplateRef<unknown>>(() => this._lazy()?.template ?? this._projected());
  public readonly index: Signal<number> = computed(() => this._stepper?.steps().indexOf(this) ?? -1);
  public readonly isActive: Signal<boolean> = computed(() => this._stepper?.activeStep() === this);
  public readonly state: Signal<StepState> = computed(() => this._stepper?.stateOf(this) ?? 'pending');
}

/**
 * Wizard container: a numbered header with one entry per step and the body of the active step. The stepper has no
 * buttons of its own; call `next()` / `previous()` from the page's actions and read `canGoNext()`, `isFirst()`, `isLast()`.
 *
 * ```html
 * <app-stepper #stepper [(selectedIndex)]="step">
 *   <app-step label="Details" [completed]="detailsValid()" [invalid]="!detailsValid()">…</app-step>
 *   <app-step label="Review">
 *     <ng-template appStepContent>…lazy…</ng-template>
 *   </app-step>
 * </app-stepper>
 * <button (click)="stepper.next()" [disabled]="!stepper.canGoNext()">Next</button>
 * ```
 */
@Component({
  selector: 'app-stepper',
  imports: [NgTemplateOutlet, TranslatePipe, LucideCheck],
  host: { class: 'block' },
  template: `
    <nav class="mb-6" [attr.aria-label]="'common.steps' | translate">
      <ol class="flex items-center gap-2 sm:gap-3">
        @for (step of steps(); track step; let i = $index; let last = $last) {
          <li class="flex min-w-0 items-center gap-2 sm:gap-3" [class.flex-1]="!last">
            <button type="button" class="group flex min-w-0 items-center gap-3 rounded-field py-1 text-left disabled:cursor-default"
              [id]="step.stepId() + '-label'" [attr.aria-current]="step.isActive() ? 'step' : null"
              [attr.aria-controls]="step.stepId() + '-panel'" [disabled]="!canSelect(i)" (click)="select(i)">
              <span class="flex size-8 shrink-0 items-center justify-center rounded-full text-sm font-semibold transition-colors"
                [class]="circleClass(step.state())">
                @switch (step.state()) {
                  @case ('done') { <svg lucideCheck class="size-4" aria-hidden="true"></svg> }
                  @case ('error') { ! }
                  @default { {{ i + 1 }} }
                }
              </span>
              <span class="hidden min-w-0 sm:block">
                <span class="block truncate text-sm font-medium" [class]="labelClass(step.state())">
                  @if (step.labelTemplate(); as label) {
                    <ng-container *ngTemplateOutlet="label.template; context: { $implicit: step }" />
                  } @else {
                    {{ step.label() || defaultLabel(i) }}
                  }
                </span>
                @if (step.hint()) {
                  <span class="hidden truncate text-xs text-base-content/60 lg:block">{{ step.hint() }}</span>
                }
              </span>
              <span class="sr-only">{{ 'common.stepState.' + step.state() | translate }}</span>
            </button>
            @if (!last) {
              <span class="h-px min-w-4 flex-1 transition-colors" aria-hidden="true"
                [class]="step.state() === 'done' ? 'bg-primary/50' : 'bg-base-300'"></span>
            }
          </li>
        }
      </ol>
      @if (activeStep(); as active) {
        <p class="mt-3 text-sm sm:hidden">
          <span class="text-base-content/60">{{ 'common.stepOf' | translate: { step: active.index() + 1, count: steps().length } }}</span>
          <span class="font-medium"> · {{ active.label() || defaultLabel(active.index()) }}</span>
        </p>
      }
    </nav>
    @for (step of steps(); track step) {
      @if (step === activeStep() || (preserveContent() && opened().has(step))) {
        <div #stepPanel role="region" tabindex="-1" class="outline-none" [id]="step.stepId() + '-panel'"
          [attr.aria-labelledby]="step.stepId() + '-label'" [hidden]="step !== activeStep()">
          <ng-container [ngTemplateOutlet]="step.content()" />
        </div>
      }
    }
  `
})
export class StepperComponent {
  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _injector = inject(Injector);
  private readonly _panels = viewChildren<ElementRef<HTMLElement>>('stepPanel');

  /** The steps, in document order. */
  public readonly steps = contentChildren(StepComponent, { descendants: true });
  /** Index of the active step; supports two-way binding. */
  public readonly selectedIndex = model(0);
  /** A step is reachable only when every step before it is completed. */
  public readonly linear = input(true);
  /** Keep the bodies of steps opened once in the DOM (hidden) so their state survives moving between steps. */
  public readonly preserveContent = input(true);
  /** Emitted when the user moves to another step. */
  public readonly selectedStepChange = output<StepComponent>();

  public readonly activeStep: Signal<StepComponent | undefined> = computed(() => {
    const steps = this.steps();
    return steps[this.selectedIndex()] ?? steps[0];
  });
  public readonly activeIndex: Signal<number> = computed(() => this.activeStep()?.index() ?? -1);
  public readonly isFirst = computed(() => this.activeIndex() <= 0);
  public readonly isLast = computed(() => this.activeIndex() === this.steps().length - 1);
  public readonly canGoNext = computed(() => !this.isLast() && this.canSelect(this.activeIndex() + 1));
  public readonly canGoBack = computed(() => !this.isFirst() && this.canSelect(this.activeIndex() - 1));

  /** Steps opened at least once; their content stays rendered while `preserveContent` is on, and their errors show. */
  protected readonly opened = linkedSignal<{ steps: readonly StepComponent[], active?: StepComponent }, ReadonlySet<StepComponent>>({
    source: () => ({ steps: this.steps(), active: this.activeStep() }),
    computation: ({ steps, active }, previous) => {
      const opened = new Set([...(previous?.value ?? [])].filter(step => steps.includes(step)));
      if (active) {
        opened.add(active);
      }
      return opened;
    }
  });

  /** Whether the step at `index` can be opened: it is editable and, in a linear stepper, every step before it is completed. */
  public canSelect(index: number): boolean {
    const steps = this.steps();
    const step = steps[index];
    if (!step || step === this.activeStep()) {
      return false;
    }
    if (!step.editable()) {
      return false;
    }
    return !this.linear() || steps.slice(0, index).every(previous => previous.completed());
  }

  /** Opens a step by index or id, when it can be opened. */
  public select(indexOrId: number | string): void {
    const steps = this.steps();
    const index = typeof indexOrId === 'number' ? indexOrId : steps.findIndex(step => step.stepId() === indexOrId);
    if (!this.canSelect(index)) {
      return;
    }
    this.selectedIndex.set(index);
    this.selectedStepChange.emit(steps[index]);
    this.revealActive();
  }

  public next(): void {
    this.select(this.activeIndex() + 1);
  }

  public previous(): void {
    this.select(this.activeIndex() - 1);
  }

  /** @internal */
  public stateOf(step: StepComponent): StepState {
    if (step === this.activeStep()) {
      return 'active';
    }
    if (step.invalid() && this.opened().has(step)) {
      return 'error';
    }
    return step.completed() ? 'done' : 'pending';
  }

  protected circleClass(state: StepState): string {
    switch (state) {
      case 'active': return 'bg-primary text-primary-content ring-4 ring-primary/15';
      case 'done': return 'bg-primary/10 text-primary group-enabled:group-hover:bg-primary/20';
      case 'error': return 'bg-error/10 text-error group-enabled:group-hover:bg-error/20';
      default: return 'border border-base-300 text-base-content/60 group-enabled:group-hover:border-base-content/40';
    }
  }

  protected labelClass(state: StepState): string {
    switch (state) {
      case 'active': return 'text-base-content';
      case 'error': return 'text-error';
      default: return 'text-base-content/70 group-enabled:group-hover:text-base-content';
    }
  }

  protected defaultLabel(index: number): string {
    return `step-${index + 1}`;
  }

  /** Moves focus to the new step's body (so screen readers announce it) and scrolls the stepper back into view. */
  private revealActive(): void {
    afterNextRender(() => {
      const index = this.activeIndex();
      const panel = this._panels().find(p => p.nativeElement.id === this.steps()[index]?.stepId() + '-panel');
      panel?.nativeElement.focus({ preventScroll: true });
      if (this._host.nativeElement.getBoundingClientRect().top < 0) {
        this._host.nativeElement.scrollIntoView({ block: 'start' });
      }
    }, { injector: this._injector });
  }
}

/** Everything needed to build a stepper, for a component's `imports`. */
export const STEPPER = [StepperComponent, StepComponent, StepLabelDirective, StepContentDirective] as const;
