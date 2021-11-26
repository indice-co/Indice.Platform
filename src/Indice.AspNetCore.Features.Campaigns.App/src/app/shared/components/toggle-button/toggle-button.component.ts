import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
    selector: 'lib-toggle-button',
    templateUrl: './toggle-button.component.html'
})
export class ToggleButtonComponent implements OnInit {
    constructor() { }

    @Input() value: boolean = false;
    @Output() valueChange: EventEmitter<boolean> = new EventEmitter(false);
    @Input('true-label') trueLabel = '';
    @Input('false-label') falseLabel = '';

    public ngOnInit(): void { }

    public toggleValue(): void {
        this.value = !this.value;
        this.valueChange.emit(this.value);
    }
}
