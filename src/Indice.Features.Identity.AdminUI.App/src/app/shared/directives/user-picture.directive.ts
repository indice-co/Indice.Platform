import { AfterContentInit, Directive, ElementRef, Input } from '@angular/core';
import * as app from 'src/app/core/models/settings';

@Directive({
    selector: 'img[userPicture]'
})
export class ImgUserPictureDirective implements AfterContentInit {
    private _userId: string;
    private _size: number = 48;
    private _displayName: string = 'John Doe';
    private _img: HTMLImageElement;

    constructor(element: ElementRef) {
        this._img = element.nativeElement as HTMLImageElement;
    }

    @Input('userPicture')
    public set setUserId(value: string) {
        this._userId = value;
    }
    
    @Input('size')
    public set setSize(value: number) {
        this._size = value;
    }

    @Input('displayName')
    public set setDisplayName(value: string) {
        this._displayName = value;
    }

    public ngAfterContentInit(): void {
        if (!this._userId) {
            return;
        }
        (async () => {
            const hash = await this.generateSHA256Hash(this._userId);
            let srcParts = ['/pictures', hash, this._size].join('/');
            let fallbackParts = ['/avatar', this._displayName, this._size].join('/');
            this._img.src=`${app.settings.auth_settings.authority}${srcParts}?d=${encodeURIComponent(fallbackParts)}`;
        })();
        
    }

    private async generateSHA256Hash(text) {
        // Encode the text as UTF-8
        const encoder = new TextEncoder();
        const data = encoder.encode(text);
    
        // Compute the hash
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
    
        // Convert the hash to a hexadecimal string
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        const hashHex = hashArray.map(byte => byte.toString(16).padStart(2, '0')).join('');
        
        return hashHex;
    }
}
