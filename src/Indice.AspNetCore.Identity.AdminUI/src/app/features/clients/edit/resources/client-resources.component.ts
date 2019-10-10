import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
    selector: 'app-client-resources',
    templateUrl: './client-resources.component.html'
})
export class ClientResourcesComponent implements OnInit {
    constructor(private _route: ActivatedRoute) { }

    public clientId = '';

    public ngOnInit(): void {
        this.clientId = this._route.parent.snapshot.params.id;
    }
}
