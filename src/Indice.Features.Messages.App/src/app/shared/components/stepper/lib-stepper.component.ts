import { Component, ContentChildren, OnInit, QueryList } from '@angular/core';

import { LibStepComponent } from './lib-step.component';

@Component({
    selector: 'lib-stepper',
    templateUrl: './lib-stepper.component.html'
})
export class LibStepperComponent implements OnInit {
    constructor() { }

    @ContentChildren(LibStepComponent, { descendants: true }) public steps!: QueryList<LibStepComponent>;

    public ngOnInit(): void { }
}
