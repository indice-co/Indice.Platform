import { Component, OnDestroy, OnInit } from "@angular/core";
import { ActivatedRoute } from "@angular/router";

import { LoggerService } from "src/app/core/services/logger.service";
import { UserStore } from "./user-store.service";
import {
  SingleUserInfo,
  UiFeaturesInfo,
} from "src/app/core/services/identity-api.service";
import { UiFeaturesService } from "src/app/core/services/ui-features.service";
import { forkJoin, map, Subscription } from "rxjs";
import { environment } from "src/environments/environment";

@Component({
  selector: "app-user-edit",
  templateUrl: "./user-edit.component.html",
  providers: [UserStore],
})
export class UserEditComponent implements OnInit, OnDestroy {
  public userId = "";
  public userName = "";
  public displayName = "";
  public signInLogsEnabled = false;

  private _getDataSubscription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private logger: LoggerService,
    private uiFeaturesService: UiFeaturesService,
    private userStore: UserStore
  ) {}

  public ngOnInit(): void {
    this.logger.log("UserEditComponent ngOnInit was called.");
    this.userId = this.route.snapshot.params["id"];

    const getFeatures = this.uiFeaturesService.getUiFeatures();
    const getUser = this.userStore.getUser(this.userId);

    this._getDataSubscription = forkJoin([getFeatures, getUser])
      .pipe(
        map((responses: [UiFeaturesInfo, SingleUserInfo]) => {
          return {
            features: responses[0],
            user: responses[1],
          };
        })
      )
      .subscribe(
        (result: { user: SingleUserInfo; features: UiFeaturesInfo }) => {
          this.signInLogsEnabled = result.features.signInLogsEnabled;

          const { userName, claims } = result.user;
          const givenName = claims.find((c) => c.type === "given_name")?.value;
          const familyName = claims.find((c) => c.type === "family_name")?.value;

          this.userName = userName;
          this.displayName = `${givenName || ''} ${familyName || ''}`.trim();
        }
      );
  }

  public ngOnDestroy(): void {
    this.logger.log("UserEditComponent ngOnDestroy was called.");
    if (this._getDataSubscription) {
      this._getDataSubscription.unsubscribe();
    }
  }
}
