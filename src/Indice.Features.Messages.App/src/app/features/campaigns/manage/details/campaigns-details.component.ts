import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';
import { NgForm } from '@angular/forms';
import { Location } from '@angular/common';

import { map } from 'rxjs/operators';
import { MenuOption, Modal, ToasterService, ToastType } from '@indice/ng-components';
import { Campaign, MessagesApiClient, MessageType, MessageTypeResultSet, UpdateCampaignRequest, ValidationProblemDetails } from 'src/app/core/services/messages-api.service';
import { UtilitiesService } from 'src/app/shared/utilities.service';

@Component({
  selector: 'app-campaigns-details',
  templateUrl: './campaigns-details.component.html'
})
export class CampaignsDetailsComponent implements OnInit {
  private _campaignId: string = '';

  constructor(
    private _api: MessagesApiClient,
    private _route: ActivatedRoute,
    private _toaster: ToasterService,
    public _utilities: UtilitiesService,
    private _location: Location) {
  }

  public customDataValid = true;
  public model: Campaign | null | undefined = null;
  public showCustomDataValidation = false;
  public now: Date = new Date();
  public messageTypes: MenuOption[] = [];
  public typeId?: string;
  public campaignTypesModalRef: Modal | undefined;

  public ngOnInit(): void {
    this.loadMessageTypes();
    this._route.parent?.params.subscribe((params: Params) => {
      this._campaignId = params.campaignId;
      this._api.getCampaignById(this._campaignId).subscribe(campaign => {
        this.model = campaign;
      });
    });
  }

  public update(): void {
    const request = {
      title: this.model?.title,
      content: this.model?.content,
      typeId: this.typeId,
      actionLink: this.model?.actionLink,
      activePeriod: this.model?.activePeriod,
      data: this.model?.data
    } as UpdateCampaignRequest;
    this._api
      .updateCampaign(this._campaignId, request)
      .subscribe({
        next: _ => {
          this._toaster.show(ToastType.Success, 'Επιτυχής επεξεργασία', `Η καμπάνια με τίτλο '${this.model?.title}' υπέστη επεξεργασία με επιτυχία.`, 5000);
          this._location.back();
        },
        error: (problemDetails: ValidationProblemDetails) => {
          this._toaster.show(ToastType.Error, 'Αποτυχία επεξεργασίας', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
        }
      });
  }

  private loadMessageTypes(): void {
    this._api
      .getMessageTypes()
      .pipe(map((messageTypes: MessageTypeResultSet) => {
        if (messageTypes.items) {
          this.messageTypes = messageTypes.items.map(type => new MenuOption(type.name || '', type.id));
          this.messageTypes.unshift(new MenuOption('Παρακαλώ επιλέξτε...', null));
        }
      }))
      .subscribe();
  }

  public setCampaignCustomData(metadataJson: string): void {
    if (!metadataJson || metadataJson === '') {
      if ('data' in this.model!) {
        delete this.model.data;
      }
      return;
    }
    try {
      const data = JSON.parse(metadataJson);
      this.customDataValid = true;
      this.model!.data = data;
    } catch (error) {
      this.customDataValid = false;
    }
  }

  public onCustomDataFocusOut(): void {
    this.showCustomDataValidation = true;
  }

  public toDate(event: any, form: NgForm): Date | undefined {
    var value = event.target.value
    if (value) {
      form.form.markAsDirty();
      return new Date(value);
    }
    return undefined;
  }

  public messageTypeSelected(selectedtypeId: string, form: NgForm) {
    if (this.model?.type) {
      this.model.type.id = selectedtypeId;
    } else {
      this.model!.type = new MessageType({ id: selectedtypeId });
    }
    this.typeId = selectedtypeId;
    form.form.markAsDirty();
  }
}
