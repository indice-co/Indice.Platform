import { Injectable } from '@angular/core';

type Dictionary = {
    [key: string]: object | undefined;
};

@Injectable({
  providedIn: 'root'
})
export class FilterCachingService {
  private filterParams: Dictionary = Object.create(null);
  constructor() { }

  /**
   * Stores params.
   */
  setParams(key: string, filterParams: object) {
    let internalKey = '$' + key;
    this.filterParams[internalKey] = filterParams;
  }

  /**
   * Gets Stored params.
   */
  getParams(key: string): object | undefined {
    let internalKey = '$' + key;
    return this.filterParams[internalKey];
  }

  /**
   * Clears Stored params.
   */
  resetParams(key: string) {
    let internalKey = '$' + key;
    delete this.filterParams[internalKey];
  }
}
