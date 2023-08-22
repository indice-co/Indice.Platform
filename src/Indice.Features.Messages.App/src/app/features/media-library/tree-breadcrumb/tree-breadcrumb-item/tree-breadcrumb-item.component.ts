import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FolderTree } from 'src/app/core/services/media-api.service';

@Component({
  selector: 'app-tree-breadcrumb-item',
  templateUrl: './tree-breadcrumb-item.component.html'
})
export class TreeBreadcrumbItemComponent implements OnInit {

  @Input() folderTree?: FolderTree;

  constructor(private _router: Router) { }

  ngOnInit(): void {
  }

  public goToFolder(id: string | undefined) {
    id ? this._router.navigate(['media', id]) : this._router.navigate(['media'])
  }

}
