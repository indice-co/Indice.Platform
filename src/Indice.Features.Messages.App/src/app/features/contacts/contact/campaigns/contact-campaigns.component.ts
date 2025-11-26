import { Component, Inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { APP_LANGUAGES, BaseListComponent, Icons, IResultSet, ListViewType, MenuOption, ViewAction } from '@indice/ng-components';
import { Observable, combineLatest, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Campaign, CampaignResultSet, MessagesApiClient } from 'src/app/core/services/messages-api.service';
import { AppLanguagesService } from 'src/app/shared/services/app-languages.service';

@Component({
    selector: 'app-contact-campaigns',
    templateUrl: './contact-campaigns.component.html',
    standalone: false
})
export class ContactCampaignsComponent extends BaseListComponent<Campaign> implements OnInit {


  constructor(
    route: ActivatedRoute,
    router: Router,
    private readonly _activatedRoute: ActivatedRoute,
    private readonly _api: MessagesApiClient,
    @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
  ) {
    super(route, router);
    this.view = ListViewType.Table;
    this.pageSize = 20;
    this.sort = 'createdAt';
    this.sortdir = 'asc';
    this.search = '';

    // Fallback initialization: use translation keys themselves as initial labels.
    this.sortOptions = [
      new MenuOption('Contacts.SortCreatedOnOption', 'createdAt'),
      new MenuOption('Contacts.SortTitleOption', 'title'),
      new MenuOption('Contacts.SortActiveFromOption', 'activePeriod.from'),
      new MenuOption('Contacts.SortTypeOption', 'type.name'),
      new MenuOption('Contacts.SortPublishedOption', 'published')
    ];
  }
  private _contactId: string = '';
  public newItemLink: string | null = null;
  public full = true;
  private _destroy$ = new Subject<void>();

  public override ngOnInit(): void {
    this._contactId = this._activatedRoute.parent?.snapshot.params['contactId'];
    super.ngOnInit();

    // Translate sort option labels reactively.
    const sortKeys = this.sortOptions.map(o => o.text);
    combineLatest(sortKeys.map(k => this._lang.translateKey(k)))
      .pipe(takeUntil(this._destroy$))
      .subscribe(translated => {
        this.sortOptions = this.sortOptions.map((o, i) => new MenuOption(translated[i] || o.text, o.value));
      });
  }

  public override ngOnDestroy(): void {
    this._destroy$.next();
    this._destroy$.complete();
  }

  public loadItems(): Observable<IResultSet<Campaign> | null | undefined> {
    return this._api
      .getCampaigns(this.page, this.pageSize, this.sortdir === 'asc' ? this.sort! : this.sort + '-', this.search || undefined, undefined, undefined, this._contactId)
      .pipe(map((result: CampaignResultSet) => (result as IResultSet<Campaign>)));
  }

  public override actionHandler(action: ViewAction): void {
    if (action.icon === Icons.Refresh) {
      this.search = '';
      this.refresh();
    }
  }
}
