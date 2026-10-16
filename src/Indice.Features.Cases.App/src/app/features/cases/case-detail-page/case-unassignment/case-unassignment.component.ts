import { Component, Input, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
    selector: 'app-case-unassignment',
    templateUrl: './case-unassignment.component.html',
    changeDetection: ChangeDetectionStrategy.Eager,
    standalone: false
})
export class CaseUnassignmentComponent implements OnInit {

  @Input()
  enabled: boolean | undefined;

  constructor() { }

  ngOnInit(): void { }  

}
