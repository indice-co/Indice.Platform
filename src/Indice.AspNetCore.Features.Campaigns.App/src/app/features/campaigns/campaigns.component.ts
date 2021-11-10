import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

import { BaseListComponent, Icons, IResultSet, ListViewType, RouterViewAction, SwitchViewAction } from '@indice/ng-components';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { SampleViewModel } from './sample.vm';

export const ShellLayoutsListSamples = [
    new SampleViewModel(
        'Custom Header shell',
        'Layout for all views in our application, contains a header component with placehodlers for actions and search',
        undefined,
        'custom-header'
    ),
    new SampleViewModel(
        'Fluid Shell layout ',
        'Layout for all model views in our application, contains a left pane navigation component with placehodlers for form components',
        undefined,
        'fluid'
    )
];

@Component({
    selector: 'app-campaigns',
    templateUrl: './campaigns.component.html'
})
export class CampaignsComponent extends BaseListComponent<SampleViewModel> implements OnInit {
    constructor(route: ActivatedRoute, router: Router) {
        super(route, router);
        this.view = ListViewType.Table;
        this.pageSize = 10;
    }

    public newItemLink: string | null = null;
    public full = true;

    public ngOnInit(): void {
        super.ngOnInit();
        this.actions = [];
        this.actions.push(new RouterViewAction(Icons.Add, 'app/campaigns/create', 'rightpane', 'Schedule new campaign'));
    }

    public loadItems(): Observable<IResultSet<SampleViewModel> | null | undefined> {
        const items: SampleViewModel[] = [];
        for (let i = 0; i < this.pageSize; i++) {
            ShellLayoutsListSamples.forEach(vm => {
                items.push(vm);
            });
        }
        return of({ count: 100, items }).pipe(delay(1200));
    }
}
