import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '@indice/ng-auth';
import { HeaderMetaItem } from '@indice/ng-components';
import { User } from 'oidc-client-ts';
import { Subscription } from 'rxjs';
import { settings } from 'src/app/core/models/settings';
import { ReportTag } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  public metaItems: HeaderMetaItem[] | null = [];
  public user: User | null = null;
  public isAdmin: boolean | undefined;
  public userSub$: Subscription | null = null;
  public reportTag = ReportTag;
  public tiles: Tiles = new Tiles();

  constructor(@Inject(AuthService) private authService: AuthService) { }

  ngOnInit(): void {
    this.authService.loadUser().subscribe((user) => {
      this.user = user;
      this.isAdmin = this.authService.isAdmin();
    }, error => {
      console.error(error);
    });
    // Detect user changes and display / or not user info accordingly...
    this.userSub$ = this.authService.user$.subscribe((user: any) => {
      this.user = user;
      this.isAdmin = this.authService.isAdmin();
    });
    this.metaItems = [];

    let tileTags = settings.dashboardTags.split(',');
    this.tiles.GroupedByCasetype = this.showTile(tileTags, this.reportTag.GroupedByCasetype);
    this.tiles.AgentGroupedByCasetype = this.showTile(tileTags, this.reportTag.AgentGroupedByCasetype);
    this.tiles.CustomerGroupedByCasetype = this.showTile(tileTags, this.reportTag.CustomerGroupedByCasetype);
    this.tiles.GroupedByStatus = this.showTile(tileTags, this.reportTag.GroupedByStatus);
    this.tiles.AgentGroupedByStatus = this.showTile(tileTags, this.reportTag.AgentGroupedByStatus);
    this.tiles.CustomerGroupedByStatus = this.showTile(tileTags, this.reportTag.CustomerGroupedByStatus);
    this.tiles.GroupedByGroupId = this.showTile(tileTags, this.reportTag.GroupedByGroupId);
  }

  showTile(tags: string[], tagId: string): boolean {
    return settings.dashboardTags === '' || tags.some(tag => tag === tagId);
  }
}

class Tiles {
  GroupedByCasetype: boolean = true;
  AgentGroupedByCasetype: boolean = true;
  CustomerGroupedByCasetype: boolean = true;
  GroupedByStatus: boolean = true;
  AgentGroupedByStatus: boolean = true;
  CustomerGroupedByStatus: boolean = true;
  GroupedByGroupId: boolean = true;
}
