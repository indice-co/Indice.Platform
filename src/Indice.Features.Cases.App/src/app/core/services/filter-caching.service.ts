import { Injectable } from '@angular/core';

type Dictionary = {
    [key: string]: object | undefined;
};

@Injectable({
  providedIn: 'root'
})
export class FilterCachingService {
  private filterParams: Dictionary = Object.create(null);
  private readonly keyPrefix = '$';
  constructor() { }

  /**
   * Stores params.
   */
  setParams(key: string, filterParams: object) {
    this.filterParams[this.keyPrefix + key] = filterParams;
  }

  /**
   * Gets Stored params.
   */
  getParams(key: string): object | undefined {
    return this.filterParams[this.keyPrefix + key];
  }

  /**
   * Clears Stored params.
   */
  resetParams(key: string) {
    delete this.filterParams[this.keyPrefix + key];
  }
}
