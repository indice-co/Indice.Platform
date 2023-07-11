import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { LoggerService } from 'src/app/core/services/logger.service';
import { UserStore } from './user-store.service';
import { IdentityApiService, UiFeaturesInfo } from 'src/app/core/services/identity-api.service';
import { UiFeaturesService } from 'src/app/core/services/ui-features.service';

@Component({
    selector: 'app-user-edit',
    templateUrl: './user-edit.component.html',
    providers: [UserStore]
})
export class UserEditComponent implements OnInit {
    constructor(private route: ActivatedRoute, private logger: LoggerService, private uiFeaturesService: UiFeaturesService) { }

    public userId = '';
    public signInLogsEnabled = false;

    public ngOnInit(): void {
        this.logger.log('UserEditComponent ngOnInit was called.');
        this.userId = this.route.snapshot.params.id;

        this.uiFeaturesService.getUiFeatures().subscribe((response: UiFeaturesInfo) => {
            this.signInLogsEnabled = response.signInLogsEnabled;
        });
    }
}
