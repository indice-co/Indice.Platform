import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { ModalService, ToasterService, ToastType } from '@indice/ng-components';
import { BasicModalComponent } from 'src/app/shared/components/basic-modal/basic-modal.component';
import { MessagesApiClient, Template } from 'src/app/core/services/messages-api.service';
import { TemplateEditStore } from '../template-edit-store.service';

@Component({
    selector: 'app-campaign-details-edit',
    templateUrl: './template-edit-details.component.html'
})
export class TemplateDetailsEditComponent implements OnInit {
    private _templateId: string | undefined;

    constructor(
        private _modalService: ModalService,
        private _api: MessagesApiClient,
        private _templateStore: TemplateEditStore,
        private _router: Router,
        @Inject(ToasterService) private _toaster: ToasterService,
        private _activatedRoute: ActivatedRoute
    ) { }

    public template: Template | undefined;

    public ngOnInit(): void {
        this._templateId = this._activatedRoute.parent?.snapshot.params['templateId'];
        if (this._templateId) {
            this._templateStore.getTemplate(this._templateId!).subscribe((template: Template) => {
                this.template = template;
            });
        }
    }

    public deleteTemplate(): void {
        const modal = this._modalService.show(BasicModalComponent, {
            animated: true,
            initialState: {
                title: 'Διαγραφή',
                message: `Είστε σίγουρος ότι θέλετε να διαγράψετε το πρότυπο '${this.template?.name}';`,
                data: this.template
            },
            keyboard: true
        });
        modal.onHidden?.subscribe((response: any) => {
            if (response.result?.answer) {
                this._api.deleteCampaign(response.result.data.id).subscribe(() => {
                    this._toaster.show(ToastType.Success, 'Επιτυχής διαγραφή', `Το πρότυπο με όνομα '${response.result.data.name}' διαγράφηκε με επιτυχία.`);
                    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['templates']));
                });
            }
        });
    }

    public openEditPane(action: string): void {
        this._router.navigate(['', { outlets: { rightpane: ['edit-template'] } }], { queryParams: { action: action } });
    }
}
