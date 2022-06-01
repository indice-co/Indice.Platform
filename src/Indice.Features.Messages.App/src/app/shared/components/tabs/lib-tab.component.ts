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
    private _uuid: string;

    constructor(
        @Inject(forwardRef(() => LibTabGroupComponent)) private _tabGroup: LibTabGroupComponent
    ) {
        this._uuid = uuid.v4();
    }

    /** The content provided for the tab. */
    @ViewChild(TemplateRef, { static: true }) public content!: TemplateRef<any>;
    /** A label for the tab header. */
    @Input() public label: string | undefined;

    public get id(): string {
        return this._uuid;
    }

    public get isActive(): boolean {
        return this._isActive;
    }

    public set isActive(isActive: boolean) {
        this._isActive = isActive;
    }

    public ngOnInit(): void { }
}