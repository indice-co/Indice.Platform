import { ChangeDetectionStrategy, Component, ContentChild, forwardRef, Inject, Input, OnChanges, OnInit, SimpleChanges, TemplateRef, ViewChild, ViewEncapsulation } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { LibStepInfo } from './lib-step-info.directive';

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
    private _interacted: boolean = false;

    constructor(
        @Inject(forwardRef(() => LibStepperComponent)) public _stepper: LibStepperComponent
    ) { }

    /** The content provided for the step. */
    @ViewChild(TemplateRef, { static: true }) public content!: TemplateRef<any>;
    /** The label of the step displayed in header, if applicable. */
    @ContentChild(LibStepLabel) public stepLabel: LibStepLabel | undefined;
    /** The info of the step displayed in header, if applicable. */
    @ContentChild(LibStepInfo) public stepInfo: LibStepInfo | undefined;
    /** An optional CSS class for the step header. */
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

    /** Indicates whether you can navigate to the step or not. */
    public get isValid(): boolean {
        if (!this.stepControl) {
            return true;
        }
        return this.stepControl.valid;
    }

    /** Shows the current state of the step. */
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

    public ngOnChanges(changes: SimpleChanges): void { }
}
