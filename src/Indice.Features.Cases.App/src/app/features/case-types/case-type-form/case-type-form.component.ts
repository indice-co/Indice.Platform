
import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-case-type-form',
  templateUrl: './case-type-form.component.html',
  styleUrls: ['./case-type-form.component.scss']
})
export class CaseTypeFormComponent implements OnInit {

  constructor() { }

  // Input & Output parameters
  @Input() public data: any = {};
  @Output() public dataChange: EventEmitter<any> = new EventEmitter();

  runOnSubmit(): void {
    this.dataChange.emit(this.data);
  }

  public jsonParse(e: any) {
    return typeof e === 'string' ? JSON.parse(e) : e;
  }

  ngOnInit(): void { }

}
