import { tap, catchError } from 'rxjs/operators';
import { CheckpointTypeRequest } from './../../core/services/cases-api.service';
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { CasesApiService, CaseTypeRequest } from "src/app/core/services/cases-api.service";
import { TailwindSubmitWidgetComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/submit-widget/submit-widget.component";
import { TailwindFrameworkComponent } from "src/app/shared/ajsf/json-schema-frameworks/tailwind-framework/tailwind-framework.component";
import { EMPTY } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class CaseTypesService {

    constructor(
        private _api: CasesApiService,
        private toaster: ToasterService,
        private router: Router) { }

    public widgets = {
        "submit": TailwindSubmitWidgetComponent
    };

    public framework = {
        framework: TailwindFrameworkComponent
    };

    public schema: any = {
        "type": "object",
        "properties": {
            "code": {
                "type": "string"
            },
            "title": {
                "type": "string"
            },
            "dataSchema": {
                "type": "string"
            },
            "layout": {
                "type": "string"
            },
            "translations": {
                "type": "string"
            },
            "layoutTranslations": {
                "type": "string"
            },
            "tags": {
                "type": "string"
            },
            "checkpointTypes": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" },
                        "description": { "type": "string" },
                        "publicStatus": { "type": "string", "enum": ["Submitted", "InProgress", "Completed", "Deleted"] },
                        "private": { "type": "boolean" }
                    },
                    "required": [
                        "name",
                        "publicStatus"
                    ]
                },
            },
            "requiresCheckpoints": {
                "type": "boolean"
            },
        },
        "additionalProperties": false,
        "required": [
            "code",
            "title",
            "dataSchema"
        ],
        "if": { "properties": { "requiresCheckpoints": { "const": true } } },
        "then": {
            "required": [
                "code",
                "title",
                "dataSchema",
                "checkpointTypes"
            ]
        }
    }

    public layout: any = [
        {
            "type": "section",
            "title": "Τύπος Αίτησης",
            "labelHtmlClass": "px-2",
            "items": [
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "code",
                            "title": "Κωδικός",
                            "htmlClass": "px-2 my-2",
                            "readonly": "true",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        },
                        {
                            "key": "title",
                            "title": "Τίτλος",
                            "htmlClass": "px-2 my-2",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        }
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "dataSchema",
                            "title": "Schema",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2",
                            "validationMessages": {
                                "required": "Υποχρεωτικό Πεδίο."
                            }
                        },
                        {
                            "key": "layout",
                            "title": "Layout",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2",
                        },
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "translations",
                            "title": "Μετάφραση",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2"
                        },
                        {
                            "key": "layoutTranslations",
                            "title": "Μετάφραση Layout",
                            "type": "textarea",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                },
                {
                    "type": "flex",
                    "flex-flow": "row wrap",
                    "items": [
                        {
                            "key": "tags",
                            "title": "Ετικέτες",
                            "htmlClass": "px-2 my-2"
                        }
                    ]
                },
                {
                    "key": "checkpointTypes",
                    "condition": "requiresCheckpoints",
                    "type": "array",
                    "listItems": 1,
                    "items": [{
                        "type": "div",
                        "displayFlex": true,
                        "flex-direction": "row",
                        "htmlClass": "px-2",
                        "items": [
                            {
                                "key": "checkpointTypes[].name", " flex": " 2 2 100px",
                                "title": "Όνομα",
                                "htmlClass": "px-2 my-2"
                            },
                            {
                                "key": "checkpointTypes[].description", " flex": " 2 2 100px",
                                "title": "Περιγραφή",
                                "htmlClass": "px-2 my-2"
                            },
                            {
                                "key": "checkpointTypes[].publicStatus", " flex": " 2 2 100px",
                                "title": "Κατάσταση",
                                "default": "Submitted",
                                "htmlClass": "px-2 my-2"
                            },
                            {
                                "key": "checkpointTypes[].private", " flex": " 2 2 100px",
                                "title": "Private",
                                "htmlClass": "px-2 mt-12"
                            }
                        ]
                    }],
                },
            ]
        }
    ]

    public onLoadLayout(id?: string): any {
        if (id == null) {
            // TODO With structuredClone https://developer.mozilla.org/en-US/docs/Web/API/structuredClone
            const createLayout = JSON.parse(JSON.stringify(this.layout))
            const activateElement = (element: any) => {
                delete element.readonly;
            }
            createLayout.forEach((element: any) => {
                activateElement(element);
                if (element.hasOwnProperty('items')) { // ajsf sections have items!
                    element.items.forEach((item: any) => {
                        activateElement(item);
                        if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
                            item.items.forEach((i: any) => {
                                activateElement(i);
                            });
                        }
                    });
                }
            });
            return createLayout;
        } else {
            return this.layout;
        }
    }

    public onCreateSubmit(event: any): void {
        const request = new CaseTypeRequest({
            title: event.title,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations,
            checkpointTypes: (event?.checkpointTypes || []).map((item: any) => new CheckpointTypeRequest(item))
        });
        this._api.createCaseType(undefined, request).pipe(
            tap(_ => {
                this.toaster.show(ToastType.Success, "Επιτυχία", "Η δημιουργία τύπου αίτησης ήταν επιτυχής.")
                this.router.navigate(['/case-types']);
            }),
            catchError(err => {
                this.toaster.show(ToastType.Error, "Whoops!", err.detail)
                return EMPTY
            })
        ).subscribe();
    }

    public onEditSubmit(caseTypeId: string, event: any) {
        const request = new CaseTypeRequest({
            id: caseTypeId,
            title: event.title,
            code: event.code,
            dataSchema: event.dataSchema,
            layout: event?.layout,
            translations: event?.translations,
            layoutTranslations: event?.layoutTranslations,
            tags: event?.tags
        })
        this._api.updateCaseType(caseTypeId, undefined, request).subscribe(_ => {
            this.toaster.show(ToastType.Success, "Επιτυχία!", "Η επεξεργασία του τύπου αίτησης ήταν επιτυχής")
        },
            (err) => {
                this.toaster.show(ToastType.Error, "Ουπς!", "Κάτι πήγε στραβά")
            })
    }
}