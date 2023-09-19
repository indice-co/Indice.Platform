import { Component, OnInit } from '@angular/core';
import { SettingsStore } from '../settings-store.service';
import { MediaSetting } from 'src/app/core/services/media-api.service';

@Component({
  selector: 'app-media-settings',
  templateUrl: './media-settings.component.html'
})
export class MediaSettingsComponent implements OnInit {

  constructor(
    private _store: SettingsStore
) {
}

  public newItemLink: string | null = 'settings';
  public settings?: MediaSetting[];

  public ngOnInit(): void {
    this._store.listMediaSettings()
      .subscribe((result) => {
        this.settings = result;
      });
  }
}
