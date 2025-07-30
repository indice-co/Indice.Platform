import { Component, OnInit } from '@angular/core';

import { BreadcrumbService } from '../../services/breadcrumb.service';
import { BreadcrumbItem } from './breadcrumb-item';

@Component({
    selector: 'my-breadcrumb',
    templateUrl: './breadcrumb.component.html'
})
export class BreadcrumbComponent implements OnInit {
    constructor(
        private _breadcrumbService: BreadcrumbService
    ) { }

    public ngOnInit(): void {
        this._breadcrumbService
            .breadcrumb
          .subscribe((breadcrumb: BreadcrumbItem[]) => {
            console.log("component");
            console.log(breadcrumb);
                this.breadcrumb = [...breadcrumb, new BreadcrumbItem('', '')];
            });
      console.log("ngOnInit:" + this.breadcrumb.length);
    }

    public breadcrumb: BreadcrumbItem[] = [];
}
