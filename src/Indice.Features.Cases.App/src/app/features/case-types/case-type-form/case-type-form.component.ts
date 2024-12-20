import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { ICaseTypeRequest } from 'src/app/core/services/cases-api.service';

@Component({
  selector: 'app-case-type-form',
  templateUrl: './case-type-form.component.html',
  styleUrls: ['./case-type-form.component.scss']
})
export class CaseTypeFormComponent implements OnInit {

  constructor() { }

  private _data: ICaseTypeRequest = {};
  
  @Input() set data(value: ICaseTypeRequest) {
    this._data = { 
      ...value 
    };
  }
  get data() {
    return this._data;
  }
  
  @Output() public dataChange: EventEmitter<any> = new EventEmitter();

  runOnSubmit(): void {
    this.dataChange.emit(this.data);
  }

  public jsonParse(e: any) {
    return typeof e === 'string' ? JSON.parse(e) : e;
  }

  ngOnInit(): void {}

}
