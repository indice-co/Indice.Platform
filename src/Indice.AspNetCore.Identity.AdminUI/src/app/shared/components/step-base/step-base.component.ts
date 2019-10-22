
import { Output, EventEmitter, Input } from '@angular/core';

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
