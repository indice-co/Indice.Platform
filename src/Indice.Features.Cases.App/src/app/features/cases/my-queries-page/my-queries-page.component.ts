import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CasesApiService, Query } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-my-queries-page',
  templateUrl: './my-queries-page.component.html'
})
export class MyQueriesPageComponent implements OnInit {
  public savedQueries: Query[] | undefined;
  public savedQuery: Query | undefined;

  constructor(
    private _api: CasesApiService,
    private _router: Router
  ) { }

  ngOnInit(): void {
    this._api.getQueries().subscribe(
      (queries: Query[]) => {
        this.savedQueries = queries;
      }
    )
  }

  applyQuery(savedQuery: Query | undefined): void {
    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
      this._router.navigateByUrl('/cases' + savedQuery!.parameters));
  }

  deleteQuery(savedQuery: Query | undefined): void {
    this._api.deleteQuery(savedQuery?.id!).subscribe(
      (_) => {
        this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
          this._router.navigateByUrl('/cases'));
      }
    )
  }

}
