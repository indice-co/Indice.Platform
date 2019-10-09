import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { LoggerService } from 'src/app/core/services/logger.service';
import { IdentityResourceStore } from './identity-resource-store.service';

@Component({
    selector: 'app-identity-resource-edit',
    templateUrl: './identity-resource-edit.component.html',
    providers: [IdentityResourceStore]
})
export class IdentityResourceEditComponent implements OnInit {
    constructor(private route: ActivatedRoute, private logger: LoggerService) { }

    public userId = '';

    public ngOnInit(): void {
        // this.userId = this.route.snapshot.params.id;
    }
}
