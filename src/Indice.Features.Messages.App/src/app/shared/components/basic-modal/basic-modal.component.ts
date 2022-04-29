import { Component } from '@angular/core';

import { Modal } from '@indice/ng-components';

@Component({
    selector: 'app-basic-modal',
    templateUrl: './basic-modal.component.html',
})
export class BasicModalComponent {
    constructor(public modal: Modal) { }

    public title: string = '';
    public data: any;

    public answer(answer: boolean): void {
        this.modal.hide({
            answer,
            data: this.data
        });
    }
}
