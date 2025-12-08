import { Component, Input, OnInit } from '@angular/core';

@Component({
    selector: 'app-case-unassignment',
    templateUrl: './case-unassignment.component.html',
    standalone: false
})
export class CaseUnassignmentComponent implements OnInit {

  @Input()
  enabled: boolean | undefined;

  constructor() { }

  ngOnInit(): void { }  

}
