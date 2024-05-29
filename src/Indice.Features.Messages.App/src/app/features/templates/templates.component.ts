import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ModalService, RouterViewAction, ToasterService, ToastType, ViewAction } from '@indice/ng-components';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { MessagesApiClient, Template, TemplateListItemResultSet } from 'src/app/core/services/messages-api.service';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';

@Component({
    selector: 'app-templates',
    templateUrl: './templates.component.html'
})
export class TemplatesComponent extends BaseListComponent<Template> implements OnInit {
    constructor(
        route: ActivatedRoute,
        private _router: Router,
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
        this.sortOptions = [new MenuOption('templates.name', 'name')];
    }

    public newItemLink: string | null = null;
    public full = true;

    public override ngOnInit(): void {
        super.ngOnInit();
        this.actions.push(new RouterViewAction(Icons.Add, 'templates/add-template', null, null));
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
                title: 'templates.edit.delete',
                message: `'templates.edit.delete-warning' '${template.name}';`,
                data: template
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteTemplate(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'templates.edit.success-delete', `'templates.edit.success-delete-message' '${response.result.data.name}' `);
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
