import { tap, catchError } from 'rxjs/operators';
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { ToasterService, ToastType } from "@indice/ng-components";
import { CasesApiService, CaseTypeRequest, CheckpointTypeDetails } from "src/app/core/services/cases-api.service";
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
                        "id": { "type": "string" },
                        "name": { "type": "string" },
                        "description": { "type": "string" },
                        "publicStatus": {
                            "type": "string",
                            "enum": ["0", "1", "2", "3"],
                            "enumNames": ["Submitted", "In Progress", "Completed", "Deleted"]
                        },
                        "private": { "type": "boolean" },
                        "roles": {
                            "type": "array",
                            "items": {
                                "type": "string"
                            }
                        }
                    },
                    "required": [
                        "name",
                        "publicStatus"
                    ]
                },
            }
        },
        "additionalProperties": false,
        "required": [
            "code",
            "title",
            "dataSchema",
            "checkpointTypes"
        ]
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
                    "type": "array",
                    "listItems": 1,
                    "items": [{
                        "type": "div",
                        "displayFlex": true,
                        "flex-direction": "column",
                        "htmlClass": "px-2 my-2",
                        "items": [
                            {
                                "type": "div",
                                "displayFlex": true,
                                "flex-direction": "row",
                                "htmlClass": "px-2",
                                "items": [{
                                    "key": "checkpointTypes[].name", " flex": " 2 2 100px",
                                    "title": "Όνομα",
                                    "htmlClass": "px-2 my-2",
                                    "readonly": "true"
                                },
                                {
                                    "key": "checkpointTypes[].description", " flex": " 2 2 100px",
                                    "title": "Περιγραφή",
                                    "htmlClass": "px-2 my-2",
                                    "readonly": "true"
                                },
                                {
                                    "key": "checkpointTypes[].publicStatus", " flex": " 2 2 100px",
                                    "title": "Κατάσταση",
                                    "default": "0",
                                    "htmlClass": "px-2 my-2",
                                    "readonly": "true"
                                },
                                {
                                    "key": "checkpointTypes[].private", " flex": " 2 2 100px",
                                    "title": "Private",
                                    "htmlClass": "px-2 mt-12",
                                    "readonly": "true"
                                }
                                ]
                            },
                            {
                                "type": "div",
                                "displayFlex": true,
                                "flex-direction": "row",
                                "htmlClass": "px-2 w-64",
                                "items": [
                                    {
                                        "key": "checkpointTypes[].roles",
                                        "flex": "flex-initial w-64",
                                        "title": "Ρόλοι",
                                        "htmlClass": "px-2 my-2",
                                        // "listItems": 1,
                                        "type": "array",
                                        "items": [
                                            {
                                                "title": "Ρόλος",
                                                "key": "checkpointTypes[].roles[]",
                                                "htmlClass": "px-2 my-2 w-64"
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    }
                    ],
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
            // TODO with recursion
            createLayout.forEach((element: any) => {
                activateElement(element);
                if (element.hasOwnProperty('items')) { // ajsf sections have items!
                    element.items.forEach((item: any) => {
                        activateElement(item);
                        if (item.hasOwnProperty('items')) { // ajsf flex containers have items!
                            item.items.forEach((i: any) => {
                                activateElement(i);
                                if (i.hasOwnProperty('items')) {
                                    i.items.forEach((j: any) => {
                                        activateElement(j);
                                    });
                                }
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
            tags: event?.tags,
            checkpointTypes: (event?.checkpointTypes || []).map((item: any) => new CheckpointTypeDetails(item)),
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
            tags: event?.tags,
            checkpointTypes: (event?.checkpointTypes || []).map((item: any) => new CheckpointTypeDetails(item)),
        })
        this._api.updateCaseType(caseTypeId, undefined, request).pipe(
            tap(_ => {
                this.toaster.show(ToastType.Success, "Επιτυχία!", "Η επεξεργασία του τύπου αίτησης ήταν επιτυχής")
            }),
            catchError(err => {
                this.toaster.show(ToastType.Error, "Whoops!", err.detail)
                return EMPTY
            })
        ).subscribe();
    }
}