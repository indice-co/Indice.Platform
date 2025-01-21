import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ProgressBarService {
  private value = new BehaviorSubject<number>(0);
  private total = new BehaviorSubject<number>(100); // Default total is 100
  private busy = new BehaviorSubject<boolean>(false);

  value$ = this.value.asObservable();
  total$ = this.total.asObservable();
  busy$ = this.busy.asObservable();

  start(total: number = 100) {
    this.busy.next(true);
    this.total.next(total);
    this.value.next(0);
  }

  setProgress(value: number) {
    this.value.next(value);
  }

  complete() {
    this.value.next(this.total.getValue()); // Ensure the progress matches the total
    setTimeout(() => {
      this.busy.next(false);
      this.value.next(0);
      this.total.next(100); // Reset the total back to default
    }, 300); // Small delay to show 100% progress
  }
}
