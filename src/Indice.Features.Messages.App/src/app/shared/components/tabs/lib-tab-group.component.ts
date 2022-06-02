import { AfterContentInit, Component, ContentChildren, OnInit, QueryList } from '@angular/core';

import { LibTabComponent } from './lib-tab.component';

@Component({
    selector: 'lib-tab-group',
    templateUrl: './lib-tab-group.component.html'
})
export class LibTabGroupComponent implements OnInit, AfterContentInit {
    public ngOnInit(): void { }

    public ngAfterContentInit(): void {
        if (!this.tabs) {
            console.warn('Please specify some tabs for lib-tab-group component');
            return;
        }
        this.tabs.get(0)!.isActive = true;
    }

    /** The inner tabs of the group. */
    @ContentChildren(LibTabComponent, { descendants: true }) public tabs: QueryList<LibTabComponent> | undefined = undefined;

    public get currentTab(): LibTabComponent | undefined {
        return this.tabs?.find(x => x.isActive);
    }

    public onTabSelected(selectedTab: LibTabComponent): void {
        this.tabs!.forEach((tab: LibTabComponent) => tab.isActive = tab.id === selectedTab.id);
    }
}