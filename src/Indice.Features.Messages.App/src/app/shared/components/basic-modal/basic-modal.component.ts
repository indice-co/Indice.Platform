import { Component, Inject, OnInit } from '@angular/core';

import { Modal, ModalOptions } from '@indice/ng-components';

@Component({
    selector: 'app-basic-modal',
    templateUrl: './basic-modal.component.html',
    styleUrls: ['./basic-modal.component.scss']
})
export class BasicModalComponent implements OnInit {
    constructor(public modal: Modal, @Inject(ModalOptions) private options: ModalOptions) { 
        this.title = options?.initialState?.title as string ?? '';
        this.message = options?.initialState?.message as string ?? '';
        this.data = options?.initialState?.data as any;
        this.type = options?.initialState?.type as 'error' | 'success' ?? 'error';
        this.acceptText = options?.initialState?.acceptText as string ?? 'Διαγραφή';
        this.class = options?.initialState?.class as string ?? '';
    }

    public title: string = '';
    public message: string = '';
    public class: string = '';
    public data: any;
    public type: 'error' | 'success' = 'error';
    public acceptText: string = 'Διαγραφή';

    public ngOnInit(): void { }

    public answer(answer: boolean): void {
        this.modal.hide({ answer, data: this.data });
    }
}
