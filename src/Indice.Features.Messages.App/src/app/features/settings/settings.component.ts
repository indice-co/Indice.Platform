import { Component, OnInit } from '@angular/core';
import { HeaderMetaItem } from '@indice/ng-components';
import { CampaignDetails } from 'src/app/core/services/messages-api.service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html'
})
export class SettingsComponent implements OnInit {

  constructor() { }

  public submitInProgress = false;
  public campaign: CampaignDetails | undefined;
  public metaItems: HeaderMetaItem[] = [];

  public ngOnInit(): void {
  }
}
