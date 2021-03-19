
import { Output, EventEmitter, Input, Component } from '@angular/core';

@Component({
    template: ''
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
