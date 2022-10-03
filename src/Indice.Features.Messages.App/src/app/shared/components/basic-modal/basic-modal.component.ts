import { Component, OnInit } from '@angular/core';

import { Modal } from '@indice/ng-components';

@Component({
    selector: 'app-basic-modal',
    templateUrl: './basic-modal.component.html',
})
export class BasicModalComponent implements OnInit {
    constructor(public modal: Modal) { }

    public title: string = '';
    public message: string = '';
    public data: any;
    public type: 'error' | 'success' = 'error';
    public acceptText: string = 'Διαγραφή';

    public ngOnInit(): void { }

    public answer(answer: boolean): void {
        this.modal.hide({ answer, data: this.data });
    }
}
