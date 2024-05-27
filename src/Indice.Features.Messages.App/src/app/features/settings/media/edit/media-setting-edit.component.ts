import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, Inject, OnInit, ViewChild } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { ToasterService, ToastType } from '@indice/ng-components';
import { SettingsStore } from '../../settings-store.service';
import { MediaSetting, UpdateMediaSettingRequest } from 'src/app/core/services/media-api.service';

@Component({
  selector: 'app-media-setting-edit',
  templateUrl: './media-setting-edit.component.html'
})
export class MediaSettingEditComponent implements OnInit, AfterViewInit {
  private _mediaSettingKey: string = '';

  constructor(
      private _changeDetector: ChangeDetectorRef,
      private _router: Router,
      private _activatedRoute: ActivatedRoute,
      private _settingsStore: SettingsStore,
      @Inject(ToasterService) private _toaster: ToasterService
  ) { }

  @ViewChild('submitBtn', { static: false }) public submitButton!: ElementRef;
  public submitInProgress = false;
  public model = new UpdateMediaSettingRequest({
    value: ''
 });

  public ngOnInit(): void {
      this._mediaSettingKey = this._activatedRoute.snapshot.params['mediaSettingKey'];
      this._settingsStore
          .getMediaSetting(this._mediaSettingKey)
          .subscribe((setting: MediaSetting | undefined) => {
            this.model.value = setting?.value
          });
  }

  public ngAfterViewInit(): void {
      this._changeDetector.detectChanges();
  }

  public onSubmit(): void {
      this.submitInProgress = true;
      this._settingsStore
          .updateMediaSettings(this._mediaSettingKey, this.model)
          .subscribe({
              next: () => {
                  this.submitInProgress = false;
                  this._toaster.show(ToastType.Success, 'settings.media.success-save', `'settings.media.success-save-message' '${this._mediaSettingKey}' `);
                  this._router.navigateByUrl('/', { skipLocationChange: true }).then(() => this._router.navigate(['settings']));
              }
          });
  }
}
