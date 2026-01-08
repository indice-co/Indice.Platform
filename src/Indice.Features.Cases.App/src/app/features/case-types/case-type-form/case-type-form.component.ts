import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CasesApiService, DecisionDefinition, DecisionRule, DecisionTable, DecisionVariableType, FieldType, RuleCondition, DecisionsResponse } from "../../../core/services/cases-api.service";

@Component({
    selector: 'app-case-type-form',
    templateUrl: './case-type-form.component.html',
    styleUrls: ['./case-type-form.component.css'],
    standalone: false
})
export class CaseTypeFormComponent implements OnInit {

    constructor(private _api: CasesApiService) {}

    private _data: any = {};
    saveErrorMessage: string | null = null;

    readonly stringOperators: Operator[] = ['==', '!='];
    readonly intOperators: Operator[] = ['==', '!=', '>', '>=', '<', '<='];

    @Input() set data(value: any) {
        this._data = { ...value };
    }
    get data() {
        return this._data;
    }
    @Output() public dataChange: EventEmitter<any> = new EventEmitter();

    runOnSubmit(): void {
        this.dataChange.emit(this.data);
    }

    public jsonParse(e: any) {
        return typeof e === 'string' ? JSON.parse(e) : e;
    }

    ngOnInit(): void {
        this.loadDecisionDefinition();
    }

    decisionTable: DecisionTable = new DecisionTable();

    // WHEN columns
    dmnWhenColumns: { name: string; type: 'string' | 'int' | 'date', allowedValues?: string[] }[] = [];

    // THEN columns
    dmnThenColumns: { name: string; type: 'string' | 'int' }[] = [
        { name: 'decision', type: 'string' }
    ];

    loadDecisionDefinition(): void {
        this._api.getDecisions('SampleAddress')
            .subscribe({
                next: (resp: DecisionsResponse) => {
                    const defs = resp.decisionDefinitions ?? [];
                    if (defs.length > 0) {
                        const def = defs[0];
                        this.decisionTable.decisionName = def.name ?? '';

                        // Map input variables to WHEN columns
                        this.dmnWhenColumns = def.variables?.map(v => ({
                            name: v.name ?? 'Unknown',
                            type: this.mapVariableTypeToInputType(v.type),
                            allowedValues: v.allowedValues
                        })) ?? [];

                        // Prefill decision table from API if available
                        if (resp.decisionTable) {
                            this.decisionTable = resp.decisionTable;
                        } else {
                            this.decisionTable.rules = [];
                        }
                    }
                },
                error: err => console.error(err)
            });
    }

    mapVariableTypeToInputType(type?: DecisionVariableType): 'string' | 'int' | 'date' {
        switch (type) {
            case 'Int':
                return 'int';
            case 'Date':
                return 'date';
            default: return 'string';
        }
    }

    addDmnRule(): void {
        this.decisionTable.decisionName = 'LoanDecision';
        const newRule = new DecisionRule();
        newRule.ruleName = `Rule-${Date.now()}`;

        newRule.conditions = this.dmnWhenColumns.map(col => {
            const cond = new RuleCondition(); // Must be an instance
            cond.field = col.name;
            cond.fieldType = col.type === 'int' ? FieldType.Int : col.type === 'date' ? FieldType.Date : FieldType.String;
            cond.operator = '==';
            cond.value = '';
            return cond;
        });

        this.decisionTable.rules = this.decisionTable.rules || [];
        this.decisionTable.rules.push(newRule);
    }

    removeDmnRule(index: number): void {
        this.decisionTable.rules?.splice(index, 1);
    }

    updateConditionValue(rule: DecisionRule, fieldName: string, value: any): void {
        const cond = rule.conditions?.find(c => c.field === fieldName);
        if (cond) cond.value = value;
    }

    updateThenValue(rule: DecisionRule, thenField: string, value: any): void {
        if (!rule.successEvent) rule.successEvent = '';
        if (thenField === 'decision') rule.successEvent = value;
    }

    saveDecisionTable(): void {
        this._api.setRules("SampleAddress", this.decisionTable)
            .subscribe({
                next: () => {
                    console.log('Decision table saved successfully');
                    this.saveErrorMessage = null;
                },
                error: err => {
                    console.error('Error saving decision table', err);
                    this.saveErrorMessage = err?.detail || 'An unexpected error occurred';
                }
            });
    }
}

type Operator = '==' | '!=' | '>' | '>=' | '<' | '<=';
