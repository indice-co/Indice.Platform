import { LibStepComponent } from '../lib-step.component';

/** Change event emitted on step changes. */
export class StepSelectedEvent {
    /** Index of the step now selected. */
    public selectedIndex!: number;
    /** Index of the step previously selected. */
    public previouslySelectedIndex: number | undefined;
    /** The step instance now selected. */
    public selectedStep!: LibStepComponent;
    /** The step instance previously selected. */
    public previouslySelectedStep: LibStepComponent | undefined;
}
