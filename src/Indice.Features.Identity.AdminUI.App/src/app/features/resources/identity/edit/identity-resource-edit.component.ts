import { Component, OnInit } from '@angular/core';

import { IdentityResourceStore } from './identity-resource-store.service';

@Component({
    selector: 'app-identity-resource-edit',
    templateUrl: './identity-resource-edit.component.html',
    providers: [IdentityResourceStore]
})
export class IdentityResourceEditComponent implements OnInit {
    constructor() { }

    public ngOnInit(): void { }
}
