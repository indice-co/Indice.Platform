import { Component, OnInit } from '@angular/core';
import { HeaderMetaItem } from '@indice/ng-components';
import { settings } from 'src/app/core/models/settings';
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
  public enableMediaLibrary = settings.enableMediaLibrary;

  public ngOnInit(): void {
  }
}
