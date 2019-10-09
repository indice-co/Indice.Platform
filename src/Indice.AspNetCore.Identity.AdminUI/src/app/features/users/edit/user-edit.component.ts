import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { LoggerService } from 'src/app/core/services/logger.service';
import { UserStore } from './user-store.service';

@Component({
    selector: 'app-user-edit',
    templateUrl: './user-edit.component.html',
    providers: [UserStore]
})
export class UserEditComponent implements OnInit {
    constructor(private route: ActivatedRoute, private logger: LoggerService) { }

    public userId = '';

    public ngOnInit(): void {
        this.logger.log('UserEditComponent ngOnInit was called.');
        this.userId = this.route.snapshot.params.id;
    }
}
