import { AfterViewChecked, ChangeDetectorRef, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { APP_LANGUAGES, HeaderMetaItem, ViewLayoutComponent } from '@indice/ng-components';
import { DistributionList } from 'src/app/core/services/messages-api.service';
import { DistributionListEditStore } from './distribution-list-edit-store.service';
import { AppLanguagesService } from '../../../shared/services/app-languages.service';
import { Subject } from 'rxjs/internal/Subject';
import { takeUntil } from 'rxjs';

@Component({
    selector: 'app-distribution-list',
    templateUrl: './distribution-list-edit.component.html',
    standalone: false
})
export class DistributionListEditComponent implements OnInit, AfterViewChecked {
    @ViewChild('layout', { static: true }) private _layout!: ViewLayoutComponent;
    private _distributionListId?: string;

    constructor(
        private _activatedRoute: ActivatedRoute,
        private _changeDetector: ChangeDetectorRef,
        private _distributionListStore: DistributionListEditStore,
        @Inject(APP_LANGUAGES) private _lang: AppLanguagesService
    ) { }
    private $destroy = new Subject<void>();
    public submitInProgress = false;
    public distributionList: DistributionList | undefined;
    public metaItems: HeaderMetaItem[] = [];

    public ngOnInit(): void {
        this._distributionListId = this._activatedRoute.snapshot.params['distributionListId'];
        if (this._distributionListId) {
            this._distributionListStore.getDistributionList(this._distributionListId!).subscribe((distributionList: DistributionList) => {
                this.distributionList = distributionList;
              this._lang.translateKey('DistributionLists.TitleFormat', { name: distributionList.name })
                .pipe(takeUntil(this.$destroy))
                .subscribe(title => {
                  this._layout.title = title || `DistributionList - ${distributionList.name}`;
                });
            });
        }
    }

    public ngAfterViewChecked(): void {
        this._changeDetector.detectChanges();
    }
    public ngOnDestroy(): void {
      this.$destroy.next();
      this.$destroy.complete();
  }

}
