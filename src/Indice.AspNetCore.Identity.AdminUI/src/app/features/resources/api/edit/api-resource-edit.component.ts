import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-api-resource-edit',
    templateUrl: './api-resource-edit.component.html'
})
export class ApiResourceEditComponent implements OnInit {
    constructor(private _route: ActivatedRoute) { }

    public clientId = '';

    public ngOnInit(): void {
        this.clientId = this._route.snapshot.params.id;
    }
}
