import { Component, HostListener, Inject, OnInit, ViewChild, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {  ToastType } from '@indice/ng-components';

import { CampaignDetails, Hyperlink, MessageContent } from 'src/app/core/services/messages-api.service';
import { CampaignContentComponent } from '../../create/steps/content/campaign-content.component';
import { CampaignEditStore } from '../campaign-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { combineLatest, Subscription } from 'rxjs';
import { AppTranslatedToaster } from '../../../../shared/services/app-translated-toaster';

@Component({
    selector: 'app-campaign-content-edit',
    templateUrl: './campaign-edit-content.component.html',
    standalone: false
})
export class CampaignContentEditComponent implements OnInit, OnDestroy {
  @ViewChild('contentStep', { static: false }) public _contentComponent: CampaignContentComponent | undefined;
  private _campaignId: string = '';

  constructor(
    private _campaignStore: CampaignEditStore,
    private _activatedRoute: ActivatedRoute,
    @Inject(AppTranslatedToaster) private _toaster: AppTranslatedToaster,
    private _lang: AppLanguagesService // injected language service
  ) { }

  public basicInfoData: any = {};
  public campaign = new CampaignDetails();
  public campaignData: any;
  public content: { [key: string]: MessageContent; } | undefined = undefined;
  public updateInProgress = false;

  // Track subscriptions for cleanup
  private _subs: Subscription[] = [];

  public ngOnInit(): void {
    this._campaignId = this._activatedRoute.parent?.snapshot.params['campaignId'];
    if (this._campaignId) {
      this._campaignStore.getCampaign(this._campaignId!).subscribe((campaign: CampaignDetails) => {
        this.campaign = campaign;
        this.basicInfoData.title = campaign.title;
        this.basicInfoData.type = campaign.type?.name;
        this.basicInfoData.actionLink = new Hyperlink({
          // Fallback initial = key (will localize if original text missing)
          text: campaign.actionLink?.text ?? 'Campaigns.ActionLinkDefaultText',
          href: campaign.actionLink?.href ?? 'https://www.indice.gr'
        });

        // Localize default action link text only when original was missing
        if (!campaign.actionLink?.text) {
          const actionLinkSub = combineLatest([
            this._lang.translateKey('Campaigns.ActionLinkDefaultText')
          ]).subscribe(([defaultText]) => {
            this.basicInfoData.actionLink.text = defaultText || 'Campaigns.ActionLinkDefaultText';
          });
          this._subs.push(actionLinkSub);
        }

        if (this.campaign.mediaBaseHref) {
          this.basicInfoData.mediaBaseHref = this.campaign.mediaBaseHref;
        }
        this.campaignData = campaign.data;
        this.content = campaign.content;
      });
    }
  }

  @HostListener('document:keydown.control.s', ['$event']) onKeydownHandler(event: KeyboardEvent) {
    event.preventDefault();
    this.updateContent();
  }

  public updateContent(): void {
    this.updateInProgress = true;
    const formContents = this._contentComponent?.form.controls.content.value;
    const content: { [key: string]: MessageContent; } = {};
    for (const item of formContents) {
      content[item.channel] = new MessageContent({
        title: item.subject,
        sender: item.sender,
        body: item.body
      });
    }
    this.campaign.mediaBaseHref = this._contentComponent?.additionalData?.mediaBaseHref;
    this.campaign.content = content;
    const data = this._contentComponent?.form.controls.data.value;
    this.campaign.data = data ? data : null;

    this._campaignStore
      .updateCampaign(this._campaignId, this.campaign)
      .subscribe(_ => {
        this.updateInProgress = false;
        // Localize toaster (title + message with {{title}} parameter)
        this._toaster.show(ToastType.Success, 'Campaigns.ContentUpdateSuccessTitle', 'Campaigns.ContentUpdateSuccessMessage', undefined, { title: this.campaign.title });

      });
  }

  public ngOnDestroy(): void {
    this._subs.forEach(s => s.unsubscribe());
  }
}
