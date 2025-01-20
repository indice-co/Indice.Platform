import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingBarService {
  private valueSubject = new BehaviorSubject<number>(0);
  private totalSubject = new BehaviorSubject<number>(100); // Default total is 100
  private busySubject = new BehaviorSubject<boolean>(false);

  value$ = this.valueSubject.asObservable();
  total$ = this.totalSubject.asObservable();
  busy$ = this.busySubject.asObservable();

  start(total: number = 100) {
    this.busySubject.next(true);
    this.totalSubject.next(total);
    this.valueSubject.next(0);
  }

  setProgress(value: number) {
    this.valueSubject.next(value);
  }

  complete() {
    this.valueSubject.next(this.totalSubject.getValue()); // Ensure the progress matches the total
    setTimeout(() => {
      this.busySubject.next(false);
      this.valueSubject.next(0);
      this.totalSubject.next(100); // Reset the total back to default
    }, 300); // Small delay to show 100% progress
  }
}
