import { ChangeDetectionStrategy, Component, forwardRef, Inject, Input, OnInit, TemplateRef, ViewChild, ViewEncapsulation } from '@angular/core';

import * as uuid from 'uuid';
import { LibTabGroupComponent } from './lib-tab-group.component';

@Component({
    selector: 'lib-tab',
    template: `
        <ng-template>
            <ng-content></ng-content>
        </ng-template>
    `,
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LibTabComponent implements OnInit {
    private _isActive: boolean = false;

    constructor(
        @Inject(forwardRef(() => LibTabGroupComponent)) private _tabGroup: LibTabGroupComponent
    ) { }

    /** The content provided for the tab. */
    @ViewChild(TemplateRef, { static: true }) public content!: TemplateRef<any>;
    /** A label for the tab header. */
    @Input() public label: string | undefined;
    /** Indicates the unique id assigned in the tab. */
    @Input() public id!: string;

    /** Indicates the index of the tab. */
    public get index(): number | undefined {
        return this._tabGroup.tabs?.toArray().indexOf(this) || undefined;
    }

    /** Indicates whether the tab is active. */
    public get isActive(): boolean {
        return this._isActive;
    }

    /** Setter for LibTabComponent isActive property. */
    public set isActive(isActive: boolean) {
        this._isActive = isActive;
    }

    public ngOnInit(): void {
        this.id = this.id || uuid.v4();
    }
}