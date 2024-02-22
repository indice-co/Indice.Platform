import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class DataService {
  private inputDataSubject = new BehaviorSubject<any>(null);
  public inputData$ = this.inputDataSubject.asObservable();

  constructor() { }

  setInputData(data: any): void {
    this.inputDataSubject.next(data);
  }
}
