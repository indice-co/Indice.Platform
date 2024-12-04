import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ModalService } from '@indice/ng-components';
import { CasesApiService, Query } from 'src/app/core/services/cases-api.service';
import { FilterCachingService } from 'src/app/core/services/filter-caching.service';
import { DeleteQueryModalComponent } from 'src/app/shared/components/delete-query-modal/delete-query-modal.component';

@Component({
  selector: 'app-queries-page',
  templateUrl: './queries-page.component.html'
})
export class QueriesPageComponent implements OnInit {
  public queries$ = this._api.getQueries();

  constructor(
    private _api: CasesApiService,
    private _router: Router,
    private _modalService: ModalService,
    private _filterCachingService: FilterCachingService
  ) { }

  ngOnInit(): void { }

  applyQuery(query: Query): void {
    this._filterCachingService.resetParams("cases");
    this.redirectTo(`/cases${query!.parameters}`);
  }

  openDeleteQueryModal(query: Query): void {
    this._modalService.show(DeleteQueryModalComponent, {
      backdrop: 'static',
      keyboard: false,
      initialState: { query: query }
    });
  }

  private redirectTo(url: string) {
    // https://stackoverflow.com/a/49509706/19162333
    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
      this._router.navigateByUrl(url));
  }

}
