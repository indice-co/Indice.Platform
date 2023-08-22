import { Component, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FolderTree, FolderTreeStructure } from 'src/app/core/services/media-api.service';

@Component({
  selector: 'app-tree-breadcrumb',
  templateUrl: './tree-breadcrumb.component.html'
})
export class TreeBreadcrumbComponent implements OnInit {

  @Input() structure?: FolderTreeStructure;

  constructor(private _router: Router) { }

  ngOnInit(): void {
  }

  public goToRoot() {
    this._router.navigate(['media']);
  }

}
