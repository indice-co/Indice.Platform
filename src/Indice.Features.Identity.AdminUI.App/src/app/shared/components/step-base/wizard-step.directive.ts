import { Directive, ViewContainerRef } from '@angular/core';

@Directive({
    selector: '[wizardStepHost]',
    standalone: false
})
export class WizardStepDirective {
    constructor(public viewContainerRef: ViewContainerRef) { }
}
