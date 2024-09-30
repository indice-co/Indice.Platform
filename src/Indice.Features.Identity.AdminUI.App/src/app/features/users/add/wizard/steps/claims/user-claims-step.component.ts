import {
  Component,
  OnInit,
  OnDestroy,
  ViewChild,
  TemplateRef,
} from "@angular/core";
import { TableColumn } from "@swimlane/ngx-datatable";
import { map, Subscription } from "rxjs";
import {
  ClaimTypeInfo,
  ClaimInfo,
  ClaimValueType,
} from "src/app/core/services/identity-api.service";
import { StepBaseComponent } from "src/app/shared/components/step-base/step-base.component";
import { UserWizardModel } from "../../models/user-wizard.model";
import { NgbDateCustomParserFormatter } from "src/app/shared/services/custom-parser-formatter.service";
import { ClaimType } from "src/app/features/users/edit/details/models/claim-type.model";
import { UserStore } from "src/app/features/users/edit/user-store.service";
import { NgbDateStruct } from "@ng-bootstrap/ng-bootstrap";

@Component({
  selector: "app-user-claims-step",
  templateUrl: "./user-claims-step.component.html",
  providers: [NgbDateCustomParserFormatter],
})
export class UserClaimsStepComponent
  extends StepBaseComponent<UserWizardModel>
  implements OnInit, OnDestroy
{
  @ViewChild("actionsTemplate", { static: true })
  private _actionsTemplate: TemplateRef<HTMLElement>;
  @ViewChild("nameTemplate", { static: true })
  public _nameTemplate: TemplateRef<HTMLElement>;
  @ViewChild("deleteAlert", { static: false })
  private _getDataSubscription: Subscription;
  private _formSubscription: Subscription;
  private _discouragedClaims: Array<string> = [
    "sub",
    "email",
    "email_verified",
    "phone_number",
    "phone_number_verified",
    "name",
  ];

  public claims: ClaimType[];
  public selectedClaimName = "";
  public selectedClaimValue: any = "";
  public selectedClaimRule = "";
  public selectedClaimValueType = ClaimValueType.String;
  public columns: TableColumn[] = [];
  public rows: ClaimInfo[] = [];

  constructor(
    private _userStore: UserStore,
    private _dateParser: NgbDateCustomParserFormatter
  ) {
    super();
  }

  public availableClaims: ClaimTypeInfo[];
  public selectedClaims: ClaimTypeInfo[];

  public ngOnInit(): void {
    this.columns = [
      {
        prop: "type",
        name: "Type",
        draggable: false,
        canAutoResize: true,
        sortable: true,
        resizeable: false,
        cellTemplate: this._nameTemplate,
      },
      {
        prop: "value",
        name: "Value",
        draggable: false,
        canAutoResize: true,
        sortable: true,
        resizeable: false,
      },
    ];
    this.columns.push({
      prop: "id",
      name: "Actions",
      draggable: false,
      canAutoResize: true,
      sortable: false,
      resizeable: false,
      cellTemplate: this._actionsTemplate,
      cellClass: "d-flex align-items-center",
    });
    const getAllClaims = this._userStore.getAllClaims();
    this._getDataSubscription = getAllClaims
      .pipe(
        map((response: ClaimTypeInfo[]) => {
          return {
            claims: response as ClaimType[],
          };
        })
      )
      .subscribe((result: { claims: ClaimType[] }) => {
        this.rows = [...this.data.form.get("claims").value];
        this.claims = result.claims.filter(
          (x) =>
            x.required === false &&
            this._discouragedClaims.indexOf(x.name) === -1
        );
      });
    this._formSubscription = this.data.form
      .get("claims")
      .valueChanges.subscribe(
        (claims: ClaimInfo[]) => (this.rows = [...claims])
      );
  }

  public ngOnDestroy(): void {
    if (this._getDataSubscription) {
      this._getDataSubscription.unsubscribe();
    }
    if (this._formSubscription) {
      this._formSubscription.unsubscribe();
    }
  }

  public claimSelected(claim: string) {
    this.selectedClaimName = claim;
    const selectedClaim = this.claims.find(
      (x) => x.name === this.selectedClaimName
    );
    if (!selectedClaim) {
      return;
    }
    this.selectedClaimRule = selectedClaim.rule;
    this.selectedClaimValueType = selectedClaim.valueType;
    this.selectedClaimValue = "";
  }

  public addClaim(): void {
    const resources = this.data.form.get("claims").value as Array<ClaimInfo>;
    const claim = new ClaimInfo({
      type: this.selectedClaimName,
      value:
        this.selectedClaimValueType === ClaimValueType.DateTime
          ? this._dateParser.format(this.selectedClaimValue as NgbDateStruct)
          : this.selectedClaimValue,
    });
    resources.push(claim);
    this.selectedClaimName = "";
    this.selectedClaimValue = "";
    this.data.form.get("claims").setValue(resources);
  }

  public deleteClaim({ type, value }: ClaimInfo): void {
    const resources = this.data.form.get("claims").value as Array<ClaimInfo>;
    const index = resources.findIndex(
      (claim) => claim.type === type && claim.value === value
    );
    if (index > -1) {
      resources.splice(index, 1);
    }
    this.data.form.get("claims").setValue(resources);
  }

  public isValid(): boolean {
    return true;
  }
}
