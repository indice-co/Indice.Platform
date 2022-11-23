import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CasesApiService, Filter } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-my-filters-page',
  templateUrl: './my-filters-page.component.html'
})
export class MyFiltersPageComponent implements OnInit {
  public savedFilters: Filter[] = [];
  public savedFilter: Filter | undefined;

  constructor(
    private _api: CasesApiService,
    private _router: Router) { }

  ngOnInit(): void {
    this._api.getFilters().subscribe(
      (savedFilters: any) => {
        this.savedFilters = savedFilters;
      }
    )
  }

  onSelect(savedFilter: Filter | undefined): void {
    this._router.navigateByUrl('/', { skipLocationChange: true }).then(() =>
      this._router.navigateByUrl('/cases' + savedFilter!.queryParameters));
  }

  onDelete(savedFilter: Filter | undefined): void {
    this._api.deleteFilter(savedFilter?.id!).subscribe(
      (_) => {
        this._router.navigate(['cases']);
      }
    )
  }
}
