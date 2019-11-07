import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { ClientStore } from './client-store.service';
import { ClientType } from 'src/app/core/services/identity-api.service';

@Component({
    selector: 'app-client-edit',
    templateUrl: './client-edit.component.html',
    providers: [ClientStore]
})
export class ClientEditComponent implements OnInit {
    constructor(private _route: ActivatedRoute) { }

    public clientId = '';

    public ngOnInit(): void {
        this.clientId = this._route.snapshot.params.id;
    }
}
