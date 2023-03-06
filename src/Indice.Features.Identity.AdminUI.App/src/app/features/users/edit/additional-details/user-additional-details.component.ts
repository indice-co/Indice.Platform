import { Component, OnInit, OnDestroy, ViewChild, TemplateRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NgForm } from '@angular/forms';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { TableColumn } from '@swimlane/ngx-datatable';
import { NgbDateStruct } from '@ng-bootstrap/ng-bootstrap';
import { UserStore } from '../user-store.service';
import { ClaimTypeInfo, SingleUserInfo, ClaimValueType, ClaimInfo } from 'src/app/core/services/identity-api.service';
import { ClaimType } from '../details/models/claim-type.model';
import { NgbDateCustomParserFormatter } from 'src/app/shared/services/custom-parser-formatter.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-user-additional-details',
    templateUrl: './user-additional-details.component.html',
    providers: [NgbDateCustomParserFormatter]
})
export class UserAdditionalDetailsComponent implements OnInit, OnDestroy {
    @ViewChild('form', { static: false }) private _form: NgForm;
    @ViewChild('actionsTemplate', { static: true }) private _actionsTemplate: TemplateRef<HTMLElement>;
    @ViewChild('nameTemplate', { static: true }) public _nameTemplate: TemplateRef<HTMLElement>;
    private _getDataSubscription: Subscription;
    private _user: SingleUserInfo;
    private _discouragedClaims: Array<string> = ['sub', 'email', 'email_verified', 'phone_number', 'phone_number_verified', 'name'];

    constructor(
        private _userStore: UserStore,
        private _route: ActivatedRoute,
        private _dateParser: NgbDateCustomParserFormatter,
        private _authService: AuthService,
        public _toast: ToastService
    ) { }

    public claims: ClaimType[];
    public selectedClaimName = '';
    public selectedClaimValue: any = '';
    public selectedClaimRule = '';
    public selectedClaimValueType = ClaimValueType.String;
    public columns: TableColumn[] = [];
    public rows: ClaimInfo[] = [];
    public canEditUser: boolean;

    public ngOnInit(): void {
        this.canEditUser = this._authService.isAdminUIUsersWriter();
        this.columns = [
            { prop: 'type', name: 'Type', draggable: false, canAutoResize: true, sortable: true, resizeable: false, cellTemplate: this._nameTemplate },
            { prop: 'value', name: 'Value', draggable: false, canAutoResize: true, sortable: true, resizeable: false }
        ];
        if (this.canEditUser) {
            this.columns.push({ prop: 'id', name: 'Actions', draggable: false, canAutoResize: true, sortable: false, resizeable: false, cellTemplate: this._actionsTemplate, cellClass: 'd-flex align-items-center' })
        }
        const userId = this._route.parent.snapshot.params.id;
        const getUser = this._userStore.getUser(userId);
        const getAllClaims = this._userStore.getAllClaims();
        this._getDataSubscription = forkJoin([getUser, getAllClaims]).pipe(map((responses: [SingleUserInfo, ClaimTypeInfo[]]) => {
            return {
                user: responses[0],
                claims: responses[1] as ClaimType[]
            };
        })).subscribe((result: { user: SingleUserInfo, claims: ClaimType[] }) => {
            this._user = result.user;
            this.rows = this._user.claims;
            this.claims = result.claims.filter(x => x.required === false && this._discouragedClaims.indexOf(x.name) === -1);
        });
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
    }

    public claimSelected(claim: string) {
        this.selectedClaimName = claim;
        const selectedClaim = this.claims.find(x => x.name === this.selectedClaimName);
        if (!selectedClaim) {
            return;
        }
        this.selectedClaimRule = selectedClaim.rule;
        this.selectedClaimValueType = selectedClaim.valueType;
    }

    public addClaim(): void {
        this._userStore.addClaim(this._user.id, {
            type: this.selectedClaimName,
            value: this.selectedClaimValueType === ClaimValueType.DateTime ? this._dateParser.format(this.selectedClaimValue as NgbDateStruct) : this.selectedClaimValue
        } as ClaimInfo).subscribe(_ => {
            this._toast.showSuccess(`Claim '${this.selectedClaimName}' was successfully added to user ${this._user.email}.`);
            this._form.resetForm({
                'claims-select': '',
                'claim-value': ''
            });
            this.rows = [...this._user.claims];
        });
    }
}
