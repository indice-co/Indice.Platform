
import { Output, EventEmitter, Input, Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
    template: '',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export abstract class StepBaseComponent<T> {
    constructor() {
        this.formValidated.subscribe((value: boolean) => {
            this.hostFormValidated = value;
        });
    }

    @Output() public formValidated = new EventEmitter<boolean>();
    @Input() public data: T;
    public hostFormValidated: boolean;
    public abstract isValid(): boolean;
}
