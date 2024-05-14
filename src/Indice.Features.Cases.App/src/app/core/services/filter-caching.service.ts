import { Injectable } from '@angular/core';

type Dictionary = {
    [key: string]: object;
};

@Injectable({
  providedIn: 'root'
})
export class FilterCachingService {
  private filterParams: Dictionary = {};

  constructor() { }

  /**
   * Stores params.
   */
  setParams(key: string, filterParams: object) {
    this.filterParams[key] = filterParams;
  }

  /**
   * Gets Stored params.
   */
  getParams(key: string): object {
    return this.filterParams[key];
  }

  /**
   * Clears Stored params.
   */
  resetParams(key: string) {
    delete this.filterParams[key];
  }
}
