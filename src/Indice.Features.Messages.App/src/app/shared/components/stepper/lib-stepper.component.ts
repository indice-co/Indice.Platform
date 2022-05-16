import { Component, ContentChildren, EventEmitter, Input, OnInit, Output, QueryList } from '@angular/core';

import { LibStepComponent, StepState } from './lib-step.component';
import { StepSelectedEvent } from './types/step-selected-event';

@Component({
    selector: 'lib-stepper',
    templateUrl: './lib-stepper.component.html'
})
export class LibStepperComponent implements OnInit {
    // Private properties.
    private _currentStepIndex: number = 0;

    constructor() { }

    /** The inner steps of the wizard. */
    @ContentChildren(LibStepComponent, { descendants: true }) public steps!: QueryList<LibStepComponent>;
    /** Emmited when a step change occurs. */
    @Output() public readonly stepChanged = new EventEmitter<StepSelectedEvent>();
    /** Indicates whether each step has to be validated before proceeding to the next. */
    @Input() public linear: boolean = false;
    public StepState = StepState;

    /** The index (starting from zero) of the current wizard step. */
    public get currentStepIndex(): number {
        return this._currentStepIndex;
    }

    public get currentStep(): LibStepComponent | undefined {
        return this.steps?.get(this._currentStepIndex);
    }

    public ngOnInit(): void { }

    /** Proceeds to the next step, if any. */
    public goToNextStep(): void {
        // Cannot go forward.
        if (this._currentStepIndex === this.steps.length - 1) {
            return;
        }
        this.updateCurrentStepIndex(this._currentStepIndex + 1);
    }

    /** Proceeds to the previous step, if any. */
    public goToPreviousStep(): void {
        // Cannot go back.
        if (this._currentStepIndex === 0) {
            return;
        }
        this.updateCurrentStepIndex(this._currentStepIndex - 1);
    }

    public onSelectStep(selectedStep: LibStepComponent): void {
        this.updateCurrentStepIndex(selectedStep.index);
    }

    private updateCurrentStepIndex(newIndex: number): void {
        const arrayOfSteps = this.steps.toArray();
        const currentStep = arrayOfSteps[this.currentStepIndex];
        if (this.linear && !currentStep.isValid) {
            return;
        }
        this.stepChanged.emit({
            selectedIndex: newIndex,
            previouslySelectedIndex: this.currentStepIndex,
            selectedStep: arrayOfSteps[newIndex],
            previouslySelectedStep: currentStep
        });
        this._currentStepIndex = newIndex;
    }
}
