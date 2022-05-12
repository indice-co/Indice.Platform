import { ChangeDetectionStrategy, Component, ContentChild, forwardRef, Inject, Input, OnChanges, OnInit, SimpleChanges, TemplateRef, ViewChild, ViewEncapsulation } from '@angular/core';
import { AbstractControl } from '@angular/forms';

import { LibStepLabel } from './lib-step-label.directive';
import { LibStepperComponent } from './lib-stepper.component';

export enum StepState {
    Active = 'Active',
    Completed = 'Completed',
    Upcoming = 'Upcoming'
};

@Component({
    selector: 'lib-step',
    template: `
        <ng-template>
            <ng-content></ng-content>
        </ng-template>
    `,
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LibStepComponent implements OnInit, OnChanges {
    constructor(
        @Inject(forwardRef(() => LibStepperComponent)) public _stepper: LibStepperComponent
    ) { }

    @ViewChild(TemplateRef, { static: true }) public content!: TemplateRef<any>;
    @ContentChild(LibStepLabel) public stepLabel: LibStepLabel | undefined;
    /** An optional CSS class for the step header. Defaults to 'bg-blue-300 group-hover:bg-blue-500'. */
    @Input() public class: string | undefined;
    /** The abstract control of the step. */
    @Input() public stepControl: AbstractControl | undefined;

    /** Indicates the index of the step. */
    public get index(): number {
        return this._stepper.steps.toArray().indexOf(this);
    }

    /** Indicates whether this step is the last step. */
    public get isLast(): boolean {
        return this._stepper.steps.length - 1 === this.index;
    }

    /** Shows the state of the step. */
    public get state(): StepState {
        const currentIndex = this._stepper.currentStepIndex;
        if (currentIndex === this.index) {
            return StepState.Active;
        }
        if (currentIndex > this.index) {
            return StepState.Completed;
        }
        return StepState.Upcoming;
    }

    public ngOnInit(): void { }

    public selectStep(): void { }

    public ngOnChanges(changes: SimpleChanges): void { }
}
