import { buildTitleMap, ErrorObject, evaluateCondition, formatErrors, isShownForChannel } from './jsonforms-utilities';

describe('evaluateCondition', () => {
    const model = { applicant: { type: 'company', vat: '' }, phones: [{ kind: 'mobile' }, { kind: 'home' }] };

    it('is true when there is no condition', () => {
        expect(evaluateCondition(undefined, model)).toBe(true);
    });

    it('resolves a dotted path to a truthy value', () => {
        expect(evaluateCondition('applicant.type', model)).toBe(true);
        expect(evaluateCondition('applicant.vat', model)).toBe(false);
        expect(evaluateCondition('model.applicant.type', model)).toBe(true);
    });

    it('evaluates == and != segments', () => {
        expect(evaluateCondition("applicant.type=='company'", model)).toBe(true);
        expect(evaluateCondition('applicant.type==person', model)).toBe(false);
        expect(evaluateCondition('applicant.type!=person', model)).toBe(true);
        expect(evaluateCondition('applicant.missing!=x', model)).toBe(true);
    });

    it('substitutes the array index of the field', () => {
        expect(evaluateCondition("phones[arrayIndex].kind=='home'", model, [1])).toBe(true);
        expect(evaluateCondition("phones[arrayIndex].kind=='home'", model, [0])).toBe(false);
    });

    it('runs a functionBody with the model and array indices', () => {
        expect(evaluateCondition({ functionBody: 'return model.applicant.type === "company";' }, model)).toBe(true);
        expect(evaluateCondition({ functionBody: 'return arrayIndices[0] === 1;' }, model, [1])).toBe(true);
    });
});

describe('buildTitleMap', () => {
    it('pairs enum values with enumNames', () => {
        expect(buildTitleMap({ enum: ['a', 'b'], enumNames: ['Alpha', 'Beta'] })).toEqual([
            { name: 'Alpha', value: 'a' },
            { name: 'Beta', value: 'b' }
        ]);
    });

    it('reads a titleMap object, filtered by enum', () => {
        expect(buildTitleMap({ titleMap: { a: 'Alpha', b: 'Beta', c: 'Gamma' }, enum: ['c', 'a'] })).toEqual([
            { name: 'Gamma', value: 'c' },
            { name: 'Alpha', value: 'a' }
        ]);
    });

    it('falls back to plain enum values', () => {
        expect(buildTitleMap({ enum: [1, 2] })).toEqual([{ name: '1', value: 1 }, { name: '2', value: 2 }]);
    });

    it('reads a oneOf of constants', () => {
        expect(buildTitleMap({ oneOf: [{ const: 'x', title: 'Ex' }, { enum: ['y'] }, { type: 'string' }] })).toEqual([
            { name: 'Ex', value: 'x' },
            { name: 'y', value: 'y' }
        ]);
    });
});

describe('formatErrors', () => {
    const error = (keyword: string, params: object = {}): ErrorObject => ({ keyword, params, message: `${keyword} failed` } as ErrorObject);

    it('uses the layout message template for the keyword', () => {
        expect(formatErrors([error('minLength', { limit: 3 })], { minLength: 'At least {{limit}} characters' })).toBe('At least 3 characters');
    });

    it('hides "required" unless it is the only error', () => {
        expect(formatErrors([error('required')], { required: 'Required field' })).toBe('Required field');
        expect(formatErrors([error('required'), error('pattern')], {})).toBe('pattern failed');
    });

    it('returns null without errors', () => {
        expect(formatErrors([], {})).toBeNull();
    });
});

describe('isShownForChannel', () => {
    it('shows fields without a channel everywhere and channel fields only for their channel', () => {
        expect(isShownForChannel({}, 'Agent')).toBe(true);
        expect(isShownForChannel({ 'case-channel': 'Agent' }, 'Agent')).toBe(true);
        expect(isShownForChannel({ 'case-channel': 'Customer' }, 'Agent')).toBe(false);
    });
});
