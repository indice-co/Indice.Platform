import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnDestroy, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { Subscription } from 'rxjs';

import { Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../../template-edit-store.service';
import { settings } from 'src/app/core/models/settings';

@Component({
    selector: 'app-campaign-details-edit-rightpane',
    templateUrl: './template-edit-details-rightpane.component.html'
})
export class TemplateDetailsEditRightpaneComponent implements OnInit, AfterViewInit, OnDestroy {
    private _updateTemplateSubscription: Subscription | undefined;
    private _templateId = '';

    constructor(
        private _templateStore: TemplateEditStore,
        private _router: Router,
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        @Inject(ToasterService) private _toaster: ToasterService
    ) { }

    @ViewChild('editNameTemplate', { static: true }) public editNameTemplate!: TemplateRef<any>;
    @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
    public submitInProgress = false;
    public templateOutlet!: TemplateRef<any>;
    public model = new Template();

    public ngOnInit(): void {
        this._templateId = this._router.url.split('/')[2];
        this._activatedRoute.queryParams.subscribe((queryParams: Params) => {
            this._selectTemplate(queryParams.action || 'editName');
        });
    }

    public ngAfterViewInit(): void {
        this._templateStore
            .getTemplate(this._templateId)
            .subscribe((template: Template) => this.model = template);
        this._changeDetector.detectChanges();
    }

    public ngOnDestroy(): void {
        this._updateTemplateSubscription?.unsubscribe();
    }

    public onSubmit(): void {
        this.submitInProgress = true;
        this._updateTemplateSubscription = this._templateStore
            .updateTemplate(this._templateId, this.model)
            .subscribe({
                next: () => {
                    this.submitInProgress = false;
                    this._toaster.show(ToastType.Success, 'templates.edit.success-save', `'templates.edit.details.pane.success-save-message' '${this.model.name}' `);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates', this._templateId]));
                }
            });
    }

    private _selectTemplate(action: string): void {
        switch (action) {
            case 'editName':
                this.templateOutlet = this.editNameTemplate;
                break;
        }
    }
}
