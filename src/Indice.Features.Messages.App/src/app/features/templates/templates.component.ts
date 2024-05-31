import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { TranslateService } from '@ngx-translate/core';
import { Observable, Subscription } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, Template, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-templates',
    templateUrl: './templates.component.html'
})
export class TemplatesComponent extends BaseListComponent<Template> implements OnInit, OnDestroy {
    private langChangeSubscription: Subscription | null = null;
    constructor(
        route: ActivatedRoute,
        private _router: Router,
        private _translate: TranslateService,
        private _api: MessagesApiClient,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _modalService: ModalService
    ) {
        super(route, _router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
        this.sort = 'name';
        this.sortdir = 'asc';
        this.search = '';
        
    }

    public newItemLink: string | null = null;
    public full = true;

    public override ngOnInit(): void {
        super.ngOnInit();
        this.actions.push(new RouterViewAction(Icons.Add, 'templates/add-template', null, null));
        this.langChangeSubscription = this._translate.onLangChange.subscribe(() => {
            this.updateMenuOptions(); 
        });
        this.updateMenuOptions(); 
    }

    public override ngOnDestroy(): void {
        if (this.langChangeSubscription) {
            this.langChangeSubscription.unsubscribe();
        }
    }

    private updateMenuOptions(): void {
        this.sortOptions = [new MenuOption(this._translate.instant('templates.name'), 'name')];
    }

    public loadItems(): Observable<IResultSet<Template> | null | undefined> {
        return this._api
            .getTemplates(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined)
            .pipe(map((result: TemplateListItemResultSet) => (result as IResultSet<Template>)));
    }

    public deleteConfirmation(template: Template): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: this._translate.instant('templates.edit.delete'),
                message: `'${this._translate.instant('templates.edit.delete-warning')}' '${template.name}';`,
                data: template
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteTemplate(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, this._translate.instant('templates.edit.success-delete'), `'${this._translate.instant('templates.edit.success-delete-message')}' '${response.result.data.name}' `);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates']));
                });
            }
        });
    }

    public override actionHandler(action: ViewAction): void {
        if (action.icon === Icons.Refresh) {
            this.search = '';
            this.refresh();
        }
    }
}
