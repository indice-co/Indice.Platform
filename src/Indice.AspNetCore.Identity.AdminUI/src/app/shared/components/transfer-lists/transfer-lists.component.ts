import { Component, Input, Output, EventEmitter } from '@angular/core';

import { TransferListsOptions } from './transfer-lists-options';

@Component({
    selector: 'app-transfer-lists',
    templateUrl: './transfer-lists.component.html',
    styleUrls: ['./transfer-lists.component.scss']
})
export class TransferListsComponent {
    @Input() public source: Array<any> = [];
    @Input() public destination: Array<any> = [];
    @Input() public titlePropertyName: string;
    @Input() public subtitlePropertyName: string;
    @Input() public options: TransferListsOptions;
    @Output() public itemAdded = new EventEmitter<any>();
    @Output() public itemRemoved = new EventEmitter<any>();

    public addItem(item: any): void {
        const index = this.source.indexOf(item, 0);
        if (index > -1) {
            this.source.splice(index, 1);
        }
        let destinationListItems = [...this.destination];
        destinationListItems.push(item);
        destinationListItems = destinationListItems.sort((x: any, y: any) => (x[this.titlePropertyName] > y[this.titlePropertyName] ? 1 : -1));
        this.destination = destinationListItems;
        this.itemAdded.emit(item);
    }

    public removeItem(item: any): void {
        const index = this.destination.indexOf(item, 0);
        if (index > -1) {
            this.destination.splice(index, 1);
        }
        let sourceListItems = [...this.source];
        sourceListItems.push(item);
        sourceListItems = sourceListItems.sort((x: any, y: any) => (x[this.titlePropertyName] > y[this.titlePropertyName] ? 1 : -1));
        this.source = sourceListItems;
        this.itemRemoved.emit(item);
    }
}
