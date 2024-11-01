import { AfterContentInit, OnInit, Directive, ElementRef, Input } from '@angular/core';
import * as app from 'src/app/core/models/settings';

@Directive({
    selector: 'img[userPicture]'
})
export class ImgUserPictureDirective implements OnInit {
    private _userId: string;
    private _size: number = 48;
    private _displayName: string = 'John Doe';
    private _color: string | undefined | null = undefined;
    private _img: HTMLImageElement;
    private _version: number = 0;

    constructor(element: ElementRef) {
        this._img = element.nativeElement as HTMLImageElement;
    }
    ngOnInit(): void {
        this.setProfileSrc();
    }

    @Input('userPicture')
    public set setUserId(value: string) {
        if (this._userId !== value) {
            this._userId = value;
            this.setProfileSrc();
        }
    }
    
    @Input('size')
    public set setSize(value: number) {
        this._size = value;
    }
    @Input('version')
    public set setVersion(value: number) {
        if (this._version !== value) {
            this._version = value;
            this.setProfileSrc();
        }
    }

    @Input('displayName')
    public set setDisplayName(value: string | { given_name: string | undefined, family_name: string | undefined, firstName: string| undefined, lastName: string| undefined, email: string| undefined, name: string| undefined, userName: string| undefined }) {
        if (!value) {
            return;
        }
        let text = this._displayName;
        if (typeof value === 'string') {
            text = value    
        } else {
            text = (value.firstName + ' ' + value.lastName).trim() ? (value.firstName + ' ' + value.lastName).trim() : 
                    (value.given_name + ' ' + value.family_name).trim() ? (value.given_name + ' ' + value.family_name).trim() :
                    value.email ? value.email :
                    value.userName ? value.userName : 
                    value.name;
        }
        this._displayName = text?.split('@')[0].replaceAll(/[\+\(\)\{\}\.,\[\]]/g, ' '); // removes any special characters
        this.setProfileSrc();
    }

    @Input('color')
    public set setColor(value: string | undefined | null) {
        this._color = value?.replace(/^#+/, '');
    }

    private setProfileSrc() {
        let fallbackParts = ['/avatar', this._displayName, this._size, this._color].filter(x => !!(x)).join('/');
        if (!this._userId) {
            let srcParts = ['/api/my/account/picture', this._size].join('/');
            this._img.src=`${app.settings.auth_settings.authority}${srcParts}?d=${encodeURIComponent(fallbackParts)}&v=${this._version}`;// create my link
            return;
        }
        (async () => {
            // create public link
            const hash = await this.generateSHA256Hash(this._userId);
            let srcParts = ['/pictures', hash, this._size].join('/');
            this._img.src=`${app.settings.auth_settings.authority}${srcParts}?d=${encodeURIComponent(fallbackParts)}&v=${this._version}`;
        })();
        this._img.alt = `Profile picture ${this._displayName}`;
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
