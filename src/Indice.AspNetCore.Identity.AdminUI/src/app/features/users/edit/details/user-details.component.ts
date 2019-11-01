import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { SingleUserInfo, ClaimTypeInfo, ValueType } from 'src/app/core/services/identity-api.service';
import { ClaimType } from './models/claim-type.model';
import { UserStore } from '../user-store.service';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-user-details',
    templateUrl: './user-details.component.html',
    providers: [NgbDateCustomParserFormatter]
})
export class UserDetailsComponent implements OnInit, OnDestroy {
    private _updateUserSubscription: Subscription;
    private _routeDataSubscription: Subscription;
    private _getDataSubscription: Subscription;

    constructor(private _route: ActivatedRoute, private _userStore: UserStore, private _dateParser: NgbDateCustomParserFormatter, public _toast: ToastService,
                private _router: Router, private _authService: AuthService) { }

    public user: SingleUserInfo;
    public requiredClaims: ClaimType[];
    public currentUserId: string;

    public ngOnInit(): void {
        this.currentUserId = this._authService.getSubjectId();
        const userId = this._route.parent.snapshot.params.id;
        const getUser = this._userStore.getUser(userId);
        const getClaims = this._userStore.getAllClaims();
        this._getDataSubscription = forkJoin([getUser, getClaims]).pipe(map((responses: [SingleUserInfo, ClaimTypeInfo[]]) => {
            return {
                user: responses[0],
                claims: responses[1] as ClaimType[]
            };
        })).subscribe((result: { user: SingleUserInfo, claims: ClaimType[] }) => {
            this.user = result.user;
            const requiredClaims = result.claims.filter(x => x.required === true);
            requiredClaims.forEach((claim: ClaimType) => {
                const userClaim = this.user.claims.find(x => x.type === claim.name);
                if (userClaim) {
                    claim.value = claim.valueType === ValueType.DateTime ? this._dateParser.parse(userClaim.value) : userClaim.value;
                }
            });
            this.requiredClaims = requiredClaims;
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._updateUserSubscription) {
            this._updateUserSubscription.unsubscribe();
        }
        if (this._routeDataSubscription) {
            this._routeDataSubscription.unsubscribe();
        }
    }

    public delete(): void {
        this._userStore.deleteUser(this.user.id).subscribe(_ => {
            this._toast.showSuccess(`User '${this.user.userName}' was deleted successfully.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        const requiredClaims = this.requiredClaims.map(x => Object.assign({}, x));
        requiredClaims.forEach((claim: ClaimType) => {
            if (claim.valueType === ValueType.DateTime) {
                const date = claim.value as NgbDateStruct;
                claim.value = this._dateParser.format(date);
            }
        });
        this._updateUserSubscription = this._userStore.updateUser(this.user, requiredClaims).subscribe(_ => {
            this._toast.showSuccess(`User '${this.user.email}' was updated successfully.`);
        });
    }
}
