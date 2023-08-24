import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { FolderTree, FolderTreeStructure } from 'src/app/core/services/media-api.service';

@Component({
  selector: 'app-tree-breadcrumb',
  templateUrl: './tree-breadcrumb.component.html'
})
export class TreeBreadcrumbComponent implements OnInit {

  @Input() structure?: FolderTreeStructure;
  public activeFolderId: string | undefined;

  constructor(private _router: Router, private _activatedRoute: ActivatedRoute) { }

  ngOnInit(): void {
    this._activatedRoute.params.subscribe((params: Params) => {
      this.activeFolderId = params.folderId;
    })
  }

  public goToRoot() {
    this._router.navigate(['media']);
  }

}
