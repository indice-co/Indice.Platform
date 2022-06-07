import { AfterContentChecked, AfterContentInit, Component, ContentChildren, EventEmitter, OnInit, Output, QueryList } from '@angular/core';

import { LibTabComponent } from './lib-tab.component';

@Component({
    selector: 'lib-tab-group',
    templateUrl: './lib-tab-group.component.html'
})
export class LibTabGroupComponent implements OnInit, AfterContentInit, AfterContentChecked {
    constructor() { }

    /** The inner tabs of the group. */
    @ContentChildren(LibTabComponent, { descendants: true }) public tabs: QueryList<LibTabComponent> | undefined = undefined;
    /** Emmited when a step change occurs. */
    @Output() public tabChanged: EventEmitter<LibTabComponent> = new EventEmitter<LibTabComponent>();

    public get currentTab(): LibTabComponent | undefined {
        return this.tabs?.find(x => x.isActive);
    }

    /** The index (starting from zero) of the current tab. */
    public get currentΤabIndex(): number | undefined {
        return this.currentTab?.index || undefined;
    }

    public ngOnInit(): void { }

    public ngAfterContentInit(): void {
        if (!this.tabs) {
            console.log('Please specify some tabs for lib-tab-group component.');
            return;
        }
    }

    public ngAfterContentChecked(): void {
        if (this.tabs && this.tabs.length > 0) {
            const anyActive = this.tabs.filter(x => x.isActive).length > 0;
            if (!anyActive) {
                this.tabs.get(0)!.isActive = true;
            }
        }
    }

    public onTabChanged(selectedTab: LibTabComponent): void {
        this.tabs!.forEach((tab: LibTabComponent) => tab.isActive = tab.id === selectedTab.id);
        this.tabChanged.emit(selectedTab);
    }
}