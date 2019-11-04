import { Injectable } from '@angular/core';

@Injectable()
export class GrantTypeStateMatrixService {
    private _stateMatrix: [{ from: string, to: string }];

    constructor() {
        this._stateMatrix = [
            { from: 'authorization_code', to: 'client_credentials' }
        ];
        this._stateMatrix.push({ from: 'authorization_code', to: 'password' });
        this._stateMatrix.push({ from: 'authorization_code', to: 'custom' });
    }

    public canGoTo(from: string, to: string): boolean {
        const index = this._stateMatrix.findIndex(x => x.from === from && x.to === to);
        return index > -1;
    }
}
