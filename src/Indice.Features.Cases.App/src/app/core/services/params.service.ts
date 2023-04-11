import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ParamsService {
  public params: any;

  constructor() { }

  /**
   * Stores params.
   */
  setParams(params: any) {
    this.params = params;
  }

  /**
   * Gets Stored params.
   */
  getParams() {
    return this.params;
  }

}
