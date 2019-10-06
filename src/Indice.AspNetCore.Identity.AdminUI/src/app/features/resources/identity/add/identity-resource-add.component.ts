import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { LoggerService } from 'src/app/core/services/logger.service';

@Component({
    selector: 'app-identity-resource-add',
    templateUrl: './identity-resource-add.component.html'
})
export class IdentityResourceAddComponent implements OnInit {
    constructor(private route: ActivatedRoute, private logger: LoggerService) { }

    public userId = '';

    public ngOnInit(): void {
        // this.userId = this.route.snapshot.params.id;
    }
}
