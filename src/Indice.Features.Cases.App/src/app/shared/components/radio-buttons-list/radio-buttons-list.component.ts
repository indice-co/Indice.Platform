import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

import { MenuOption } from '@indice/ng-components';

@Component({
    selector: 'lib-radio-buttons-list',
    templateUrl: './radio-buttons-list.component.html'
})
export class RadioButtonsListComponent implements OnInit {
    constructor() { }

    @Input() title: string | undefined;
    @Input() value: any | undefined;
    @Input() options: MenuOption[] | undefined;
    @Output() valueChange: EventEmitter<any> = new EventEmitter(null as any);

    public ngOnInit(): void { }

    public selectedChanged(option: MenuOption) {
        this.value = option.value;
        this.valueChange.emit(this.value);
    }
}
