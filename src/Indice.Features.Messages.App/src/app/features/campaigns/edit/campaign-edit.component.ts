import { AfterViewChecked, ChangeDetectorRef, Component, OnInit, ViewChild, OnDestroy, Inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { APP_LANGUAGES, HeaderMetaItem, Icons, ViewLayoutComponent } from '@indice/ng-components';
import { CampaignDetails } from 'src/app/core/services/messages-api.service';
import { CampaignEditStore } from './campaign-edit-store.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';
import { combineLatest, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
    selector: 'app-campaign-edit',
    templateUrl: './campaign-edit.component.html',
    standalone: false
})
export class CampaignEditComponent implements OnInit, AfterViewChecked, OnDestroy {
  @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
  private _campaignId?: string;
  private readonly _destroy$ = new Subject<void>();

  constructor(
    private _activatedRoute: ActivatedRoute,
    private _campaignStore: CampaignEditStore,
    private _router: Router,
    private _changeDetector: ChangeDetectorRef,
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
  ) { }

  public submitInProgress = false;
  public campaign: CampaignDetails | undefined;
  public metaItems: HeaderMetaItem[] = [];

  public ngOnInit(): void {
    this._campaignId = this._activatedRoute.snapshot.params['campaignId'];
    if (this._campaignId) {
      this._campaignStore.getCampaign(this._campaignId!).pipe(takeUntil(this._destroy$)).subscribe((campaign: CampaignDetails) => {
        this.campaign = campaign;
        const titleParam = { title: campaign.title };
        const dateString = new Date();
        const dateParam = { date: dateString };

        const layoutTitle$ = this._lang.translateKey('Campaigns.EditTitleFormat', titleParam);
        const status$ = campaign.published
          ? this._lang.translateKey('Campaigns.PublishedAtInfo', dateParam)
          : this._lang.translateKey('Campaigns.StatusUnpublished');

        combineLatest([layoutTitle$, status$])
          .subscribe(([translatedTitle, translatedStatus]) => {
            this._layout.title = translatedTitle || 'Campaigns.EditTitleFormat';
            this.metaItems = []; // reset to avoid duplicates on language change
            if (campaign.published) {
              this.metaItems.push({
                key: 'status',
                icon: Icons.Heart,
                text: translatedStatus || 'Campaigns.PublishedAtInfo'
              });
            } else {
              this.metaItems.push({
                key: 'status',
                icon: Icons.HeartBroken,
                text: translatedStatus || 'Campaigns.StatusUnpublished'
              });
            }
          }).unsubscribe();
      });
    }
  }

  public ngAfterViewChecked(): void {
    this._changeDetector.detectChanges();
  }

  public ngOnDestroy(): void {

  }
}
