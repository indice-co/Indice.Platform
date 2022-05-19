import { Directive, TemplateRef } from '@angular/core';

@Directive({
    selector: '[libStepInfo]',
})
export class LibStepInfo {
    constructor(public template: TemplateRef<any>) { }
}
