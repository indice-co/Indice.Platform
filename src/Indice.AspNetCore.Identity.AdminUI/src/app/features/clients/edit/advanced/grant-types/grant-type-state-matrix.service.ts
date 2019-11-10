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
        this._stateMatrix.push({ from: 'client_credentials', to: 'authorization_code' });
        this._stateMatrix.push({ from: 'client_credentials', to: 'implicit' });
        this._stateMatrix.push({ from: 'client_credentials', to: 'password' });
        this._stateMatrix.push({ from: 'client_credentials', to: 'hybrid' });
        this._stateMatrix.push({ from: 'client_credentials', to: 'custom' });
        this._stateMatrix.push({ from: 'client_credentials', to: 'urn:ietf:params:oauth:grant-type:device_code' });
        this._stateMatrix.push({ from: 'hybrid', to: 'client_credentials' });
        this._stateMatrix.push({ from: 'hybrid', to: 'password' });
        this._stateMatrix.push({ from: 'hybrid', to: 'custom' });
        this._stateMatrix.push({ from: 'password', to: 'implicit' });
        this._stateMatrix.push({ from: 'password', to: 'authorization_code' });
        this._stateMatrix.push({ from: 'password', to: 'hybrid' });
        this._stateMatrix.push({ from: 'password', to: 'client_credentials' });
        this._stateMatrix.push({ from: 'password', to: 'custom' });
    }

    public canGoTo(from: string, to: string): boolean {
        const index = this._stateMatrix.findIndex(x => x.from === from && x.to === to);
        return index > -1;
    }
}
