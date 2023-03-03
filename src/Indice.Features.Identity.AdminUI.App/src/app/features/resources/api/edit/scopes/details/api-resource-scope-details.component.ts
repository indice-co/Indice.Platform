import { Component, OnInit, OnDestroy, ViewChild, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Subscription } from 'rxjs';
import { SwalComponent } from '@sweetalert2/ngx-sweetalert2';
import { ApiScopeInfo, ApiScopeTranslation } from 'src/app/core/services/identity-api.service';
import { ToastService } from 'src/app/layout/services/app-toast.service';
import { ApiResourceStore } from '../../../api-resource-store.service';
import { UtilitiesService } from 'src/app/core/services/utilities.services';
import { AuthService } from 'src/app/core/services/auth.service';
import { TranslateInputService } from 'src/app/shared/components/translate-input/translate-input.service';

@Component({
    selector: 'app-api-resource-scope-details',
    templateUrl: './api-resource-scope-details.component.html',
    providers: [TranslateInputService]
})
export class ApiResourceScopeDetailsComponent implements OnInit, OnDestroy {
    @ViewChild('deleteAlert', { static: false }) private _deleteAlert: SwalComponent;
    private _getDataSubscription: Subscription;
    private _deleteApiResourceSubscription: Subscription;
    private _updateApiResourceSubscription: Subscription;
    private _apiResourceId: number;

    constructor(
        private _route: ActivatedRoute,
        private _apiResourceStore: ApiResourceStore,
        public _toast: ToastService,
        public utilities: UtilitiesService,
        private _authService: AuthService,
        private _translateInputService: TranslateInputService
    ) { }

    @Input() public scope = new ApiScopeInfo();
    @Input() public editable = false;
    public discriminator = this.utilities.newGuid();
    public canEditResource: boolean;
    public selectedCulture: string;
    public cultures = ['EL', 'EN'];
    public nameTranslations: { [key: string]: string; } = {};
    public descriptionTranslations: { [key: string]: string; } = {};

    public ngOnInit(): void {
        this.canEditResource = this._authService.isAdminUIClientsWriter();
        this._apiResourceId = +this._route.parent.snapshot.params.id;
        if (!this.scope.translations) {
            this.scope.translations = {} as { [key: string]: ApiScopeTranslation; };
            return;
        }
        this.nameTranslations = this._translateInputService.getPropertyTranslations('displayName', this.scope);
        this.descriptionTranslations = this._translateInputService.getPropertyTranslations('description', this.scope);
    }

    public ngOnDestroy(): void {
        if (this._getDataSubscription) {
            this._getDataSubscription.unsubscribe();
        }
        if (this._deleteApiResourceSubscription) {
            this._deleteApiResourceSubscription.unsubscribe();
        }
        if (this._updateApiResourceSubscription) {
            this._updateApiResourceSubscription.unsubscribe();
        }
    }

    public deletePrompt(scope: ApiScopeInfo): void {
        this.scope = scope;
        setTimeout(() => this._deleteAlert.fire(), 0);
    }

    public delete(): void {
        this._deleteApiResourceSubscription = this._apiResourceStore.deleteApiResourceScope(this._apiResourceId, this.scope.id).subscribe(_ => {
            this._toast.showSuccess(`API scope '${this.scope.name}' was deleted successfully.`);
        });
    }

    public update(): void {
        this._updateApiResourceSubscription = this._apiResourceStore.updateApiResourceScope(this._apiResourceId, this.scope).subscribe(_ => {
            this._toast.showSuccess(`API scope '${this.scope.name}' was updated successfully.`);
        });
    }

    public onCultureChanged(culture: string) {
        this.selectedCulture = culture;
    }

    public getDisplayNameTranslations(translations: { [key: string]: string; }): void {
        this.setPropertyTranslation('displayName', translations, this.scope);
    }

    public getDescriptionTranslations(translations: { [key: string]: string; }): void {
        this.setPropertyTranslation('description', translations, this.scope);
    }

    public setPropertyTranslation(propertyPath: string | Array<string>, translations: { [key: string]: string; }, obj: any): void {
        this._translateInputService.setPrimaryLanguageTranslation(propertyPath, translations, obj);
        this._translateInputService.setOtherLanguageTranslation(propertyPath, translations, obj, true);
    }
}
