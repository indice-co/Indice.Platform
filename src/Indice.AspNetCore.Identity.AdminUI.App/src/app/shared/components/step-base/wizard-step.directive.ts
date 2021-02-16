import { Directive, ViewContainerRef } from '@angular/core';

@Directive({
    selector: '[wizardStepHost]',
})
export class WizardStepDirective {
    constructor(public viewContainerRef: ViewContainerRef) { }
}
