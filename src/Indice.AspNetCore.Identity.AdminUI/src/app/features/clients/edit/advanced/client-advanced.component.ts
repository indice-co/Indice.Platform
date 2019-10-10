import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-client-advanced',
    templateUrl: './client-advanced.component.html'
})
export class ClientAdvancedComponent implements OnInit {
    constructor(private _route: ActivatedRoute) { }

    public clientId = '';

    public ngOnInit(): void {
        this.clientId = this._route.snapshot.params.id;
    }
}
