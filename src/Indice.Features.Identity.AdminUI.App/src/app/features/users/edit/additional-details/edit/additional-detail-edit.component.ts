import { Component, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { ClaimType } from '../../details/models/claim-type.model';
import { UserStore } from '../../user-store.service';
import { SingleUserInfo, ClaimTypeInfo, ClaimValueType } from 'src/app/core/services/identity-api.service';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';

@Component({
    selector: 'app-additional-detail-edit',
    templateUrl: './additional-detail-edit.component.html',
    providers: [NgbDateCustomParserFormatter]
})
export class AdditionalDetailEditComponent implements OnInit, OnDestroy {
    private _getDataSubscription: Subscription;
    private _userId: string;
    private _claimId: number;
    private _userEmail: string;

    constructor(private _userStore: UserStore, private _router: Router, private _route: ActivatedRoute, private _dateParser: NgbDateCustomParserFormatter, public _toast: ToastService) { }

    public claim: ClaimType;

    public ngOnInit(): void {
        this._userId = this._route.snapshot.parent.params['id'];
        this._claimId = +this._route.snapshot.params['id'];
        const getUser = this._userStore.getUser(this._userId);
        const getClaims = this._userStore.getAllClaims();
        this._getDataSubscription = forkJoin([getUser, getClaims]).pipe(map((responses: [SingleUserInfo, ClaimTypeInfo[]]) => {
            return {
                user: responses[0],
                claims: responses[1] as ClaimType[]
            };
        })).subscribe((result: { user: SingleUserInfo, claims: ClaimType[] }) => {
            this._userEmail = result.user.email;
            const userClaim = result.user.claims.find(x => x.id === this._claimId);
            const claimType = userClaim && result.claims.find(x => x.name === userClaim.type);
            if (claimType) {
                const claim = claimType as ClaimType;
                claim.value = claim.valueType === ClaimValueType.DateTime ? this._dateParser.parse(userClaim.value) : userClaim.value;
                this.claim = claim;
            }
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public delete(): void {
        this._userStore.deleteUserClaim(this._userId, this._claimId).subscribe(_ => {
            this._toast.showSuccess(`Claim '${this.claim.name}' was successfully removed from user '${this._userEmail}'.`);
            this._router.navigate(['../../'], { relativeTo: this._route });
        });
    }

    public update(): void {
        const claimValue = this.claim.valueType === ClaimValueType.DateTime ? this._dateParser.format(this.claim.value as NgbDateStruct) : this.claim.value;
        this._userStore.updateUserClaim(this._userId, this._claimId, claimValue).subscribe(_ => {
            this._toast.showSuccess(`Claim '${this.claim.name}' was successfully updated.`);
        });
    }
}
