import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CasesApiService, Query } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-queries-page',
  templateUrl: './queries-page.component.html'
})
export class QueriesPageComponent implements OnInit {
  public queries$ = this._api.getQueries();

  constructor(
    private _api: CasesApiService,
    private _router: Router
  ) { }

  ngOnInit(): void { }

  applyQuery(query: Query): void {
    this.redirectTo(`/cases${query!.parameters}`);
  }

  deleteQuery(query: Query): void {
    this._api.deleteQuery(query?.id!).subscribe(
      (_) => {
        this.redirectTo('/cases');
      }
    )
  }

  private redirectTo(url: string) {
    // https://stackoverflow.com/a/49509706/19162333
    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
      this._router.navigateByUrl(url));
  }

}
