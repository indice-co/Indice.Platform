import { Type } from '@angular/core';

export class WizardStepDescriptor {
    constructor(
        public title: string,
        public component: Type<any>
    ) { }
}
