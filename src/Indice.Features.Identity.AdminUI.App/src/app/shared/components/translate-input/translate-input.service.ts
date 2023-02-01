import { Injectable } from '@angular/core';

@Injectable()
export class TranslateInputService {
    private _primaryLanguage = 'EL';

    public setPrimaryLanguageTranslation(propertyPath: string | Array<string>, translations: { [key: string]: string; }, obj: any, includePrimaryLangIntoTranslations = false): void {
        const properties = Array.isArray(propertyPath) ? propertyPath : propertyPath.split('.');
        const value = translations[this._primaryLanguage];
        if (value) {
            if (properties.length > 1) {
                if (!obj.hasOwnProperty(properties[0]) || typeof obj[properties[0]] !== 'object') {
                    obj[properties[0]] = {};
                }
                return this.setPrimaryLanguageTranslation(properties.slice(1), translations, obj[properties[0]]);
            } else {
                obj[`${propertyPath}`] = value;
                if (includePrimaryLangIntoTranslations) {
                    if (!obj.translations) {
                        obj.translations = {};
                    }
                    if (!obj.translations[this._primaryLanguage]) {
                        obj.translations[this._primaryLanguage] = {};
                    }
                    obj.translations[this._primaryLanguage][`${propertyPath}`] = value;
                }
            }
        }
    }

    public setOtherLanguageTranslation(propertyPath: string | Array<string>, translations: { [key: string]: string; }, obj: any, isRecursive: boolean = false): void {
        const properties = Array.isArray(propertyPath) ? propertyPath : propertyPath.split('.');
        for (const language in translations) {
            if (translations.hasOwnProperty(language) && language !== this._primaryLanguage) {
                const value = translations[language];
                if (isRecursive === true) {
                    if (!obj.translations) {
                        obj.translations = {};
                    }
                    if (!obj.translations[language]) {
                        obj.translations[language] = {};
                    }
                }
                if (properties.length > 1) {
                    if (!obj.translations[language].hasOwnProperty(properties[0]) || typeof obj.translations[language][properties[0]] !== 'object') {
                        obj.translations[language][properties[0]] = {};
                    }
                    return this.setOtherLanguageTranslation(properties.slice(1), translations, obj.translations[language][properties[0]]);
                } else {
                    if (isRecursive) {
                        obj.translations[language][`${propertyPath}`] = value;
                    } else {
                        obj[`${propertyPath}`] = value;
                    }
                }
            }
        }
    }

    public getPropertyTranslations(propertyPath: string | Array<string>, obj: any): { [key: string]: string; } {
        const propertyTranslations: { [key: string]: string; } = {};
        for (const culture in obj.translations) {
            if (obj.translations.hasOwnProperty(culture)) {
                const propertyTranslation = this.getPropertyTranslation(propertyPath, culture, obj);
                propertyTranslations[culture] = propertyTranslation;
            }
        }
        return propertyTranslations;
    }

    public getPropertyTranslation(propertyPath: string | Array<string>, culture: string, obj: any, isRecursive: boolean = false): any {
        if (obj == null) {
            return undefined;
        }
        const properties = Array.isArray(propertyPath) ? propertyPath : propertyPath.split('.');
        if (properties.length > 1) {
            if (obj.translations[culture].hasOwnProperty(properties[0])) {
                return this.getPropertyTranslation(properties.slice(1), culture, obj.translations[culture][properties[0]], true);
            } else {
                return undefined;
            }
        }
        return !isRecursive ? obj.translations[culture][`${propertyPath}`] : obj[`${propertyPath}`];
    }
}