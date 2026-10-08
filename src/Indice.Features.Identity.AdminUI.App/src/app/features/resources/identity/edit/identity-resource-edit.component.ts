import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

import { IdentityResourceStore } from './identity-resource-store.service';

@Component({
    selector: 'app-identity-resource-edit',
    templateUrl: './identity-resource-edit.component.html',
    providers: [IdentityResourceStore],
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class IdentityResourceEditComponent implements OnInit {
    constructor() { }

    public ngOnInit(): void { }
}
