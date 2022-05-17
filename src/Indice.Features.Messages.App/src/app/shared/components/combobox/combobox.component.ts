import { Component, EventEmitter, Input, OnInit, Output, TemplateRef } from '@angular/core';

import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';

@Component({
    selector: 'lib-combobox',
    templateUrl: './combobox.component.html'
})
export class ComboboxComponent implements OnInit {
    private _debouncer: Subject<string> = new Subject<string>();
    // private _itemsValueChange: EventEmitter<any[]> = new EventEmitter<any[]>();
    private _items: any[] = [];

    private _selectedItemsFilter = (item: any) => {
        const selectedItem = this.selectedItems.find(x => x.recipientId == item.recipientId);
        return selectedItem == null || selectedItem == undefined;
    };

    constructor() { }

    @Input() public id: string = 'combobox';

    @Input('items') public set items(items: any[]) {
        this._items = items.filter(this._selectedItemsFilter);
    }

    public get items(): any[] {
        return this._items.filter(this._selectedItemsFilter);
    }

    @Input() public itemTemplate: TemplateRef<HTMLElement> | null = null;
    @Input() public selectedItemTemplate: TemplateRef<HTMLElement> | null = null;
    @Input() public busy: boolean = false;
    @Input() public debounceMs: number = 1000;
    @Output() public onSearch: EventEmitter<string | undefined> = new EventEmitter();
    @Output() public onItemSelected: EventEmitter<any> = new EventEmitter();
    public showOptions: boolean = false;
    public selectedItems: any[] = [];

    public ngOnInit(): void {
        this.emitSearchEvent();
        this._debouncer
            .pipe(
                debounceTime(this.debounceMs),
                distinctUntilChanged()
            )
            .subscribe((value: string) => this.emitSearchEvent(value));
    }

    public onInputClick(): void {
        this.showOptions = true;
    }

    public onInputClickOutside(): void {
        this.showOptions = false;
    }

    public onInputKeyUp(event: any): void {
        this._debouncer.next(event.currentTarget.value);
    }

    public onListItemSelected(item: any): void {
        this.onItemSelected.emit(item);
        this.selectedItems.push(item);
    }

    public removeSelectedItem(item: any): void {
        const index = this.selectedItems.indexOf(item);
        if (index > -1) {
            this.selectedItems.splice(index, 1);
        }
    }

    private emitSearchEvent(searchTerm: string | undefined = undefined): void {
        this.onSearch.emit(searchTerm);
    }
}
