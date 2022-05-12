import { Directive, TemplateRef } from '@angular/core';

@Directive({
    selector: '[libStepLabel]',
})
export class LibStepLabel {
    constructor(public template: TemplateRef<any>) { }
}
