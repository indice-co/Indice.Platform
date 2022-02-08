import { NgForm } from '@angular/forms';
import { Campaign, CampaignsApiService, CampaignType, CampaignTypeResultSet, UpdateCampaignRequest, ValidationProblemDetails } from 'src/app/core/services/campaigns-api.services';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MenuOption, Modal, ModalService, ToasterService, ToastType } from '@indice/ng-components';
import { UtilitiesService } from 'src/app/shared/utilities.service';
import { Location } from '@angular/common';
import { map } from 'rxjs/operators';
import { CampaignTypesModalComponent } from '../../campaign-types-modal/campaign-types.component';

@Component({
  selector: 'app-campaigns-details',
  templateUrl: './campaigns-details.component.html'
})
export class CampaignsDetailsComponent implements OnInit {

  constructor(
    private _api: CampaignsApiService,
    private modal: ModalService,
    private route: ActivatedRoute,
    private _toaster: ToasterService,
    public _utilities: UtilitiesService,
    private _location: Location) {
  }

  private _campaignId: string = '';
  public customDataValid = true;
  public model: Campaign | null | undefined = null;
  public showCustomDataValidation = false;
  public now: Date = new Date();
  public campaignTypes: MenuOption[] = [];
  public typeId?: string;
  public campaignTypesModalRef: Modal | undefined;

  ngOnInit(): void {
    this.loadCampaignTypes();
    this.route.parent?.params.subscribe(p => {
      this._campaignId = p.campaignId;
      this._api.getCampaignById(this._campaignId).subscribe(campaign => {
        this.model = campaign;
      });
    });
  }

  public update() {
    this._api.updateCampaign(this._campaignId, {
      title: this.model?.title,
      content: this.model?.content,
      typeId: this.typeId,
      actionText: this.model?.actionText,
      published: this.model?.published,
      activePeriod: this.model?.activePeriod,
      data: this.model?.data
    } as UpdateCampaignRequest).subscribe(_ => {
      this._toaster.show(ToastType.Success, 'Επιτυχής επεξεργασία', `Η καμπάνια με τίτλο '${this.model?.title}' υπέστη επεξεργασία με επιτυχία.`, 5000);
      this._location.back();
    }, (problemDetails: ValidationProblemDetails) => {
      this._toaster.show(ToastType.Error, 'Αποτυχία επεξεργασίας', `${this._utilities.getValidationProblemDetails(problemDetails)}`, 6000);
    });
  }

  private loadCampaignTypes(): void {
    this.campaignTypes = [];
    this._api
      .getCampaignTypes()
      .pipe(map((campaignTypes: CampaignTypeResultSet) => {
        if (campaignTypes.items) {
          this.campaignTypes = campaignTypes.items.map(type => new MenuOption(type.name || '', type.id));
          this.campaignTypes.unshift(new MenuOption('Παρακαλώ επιλέξτε...', null));
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

  public openCampaignTypesModal(): void {
    this.campaignTypesModalRef = this.modal.show(CampaignTypesModalComponent, {
      backdrop: 'static',
      keyboard: false,
      animated: true,
      initialState: {
        campaignTypes: this.campaignTypes.filter(x => x.value != null)
      }
    });
    this.campaignTypesModalRef.onHidden?.subscribe((response: any) => {
      if (response.result.campaignTypesChanged) {
        this.loadCampaignTypes();
      }
    });
  }

  public typeSelected(selectedtypeId: string, form : NgForm) {
    if (this.model?.type) {
      this.model.type.id = selectedtypeId;
    } else {
      this.model!.type = new CampaignType({
        id: selectedtypeId
      })
    }
    this.typeId = selectedtypeId;
    form.form.markAsDirty();
  }
}
