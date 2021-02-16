import { Component, OnInit, OnDestroy } from '@angular/core';

import { Subscription, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { IdentityApiService, BlogItemInfo, BlogItemInfoResultSet, SummaryInfo } from 'src/app/core/services/identity-api.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private _getDataSubscription: Subscription;
  private _getNewsSubscription: Subscription;
  private _postsToLoad = 9;
  private _currentPostsPage = 1;

  constructor(private _api: IdentityApiService) { }

  public blogItems: BlogItemInfo[] = [];
  public totalNumberOfPosts = 0;
  public summary = new SummaryInfo();

  public ngOnInit(): void {
    const getSummary = this._api.getSystemSummary();
    const getNews = this._api.getNews(this._currentPostsPage, this._postsToLoad);
    this._getDataSubscription = forkJoin([getSummary, getNews]).pipe(map((responses: [SummaryInfo, BlogItemInfoResultSet]) => {
      return {
        summary: responses[0],
        posts: responses[1]
      };
    })).subscribe((result: { summary: SummaryInfo, posts: BlogItemInfoResultSet }) => {
      this.totalNumberOfPosts = result.posts.count;
      this.blogItems = result.posts.items;
      this.summary = result.summary;
    });
  }

  public ngOnDestroy(): void {
    if (this._getDataSubscription) {
      this._getDataSubscription.unsubscribe();
    }
    if (this._getNewsSubscription) {
      this._getNewsSubscription.unsubscribe();
    }
  }

  public loadNextPosts(): void {
    this._currentPostsPage++;
    this._getNewsSubscription = this._api.getNews(this._currentPostsPage, this._postsToLoad).subscribe((response: BlogItemInfoResultSet) => {
      this.totalNumberOfPosts = response.count;
      this.blogItems.push(...response.items);
    });
  }
}
