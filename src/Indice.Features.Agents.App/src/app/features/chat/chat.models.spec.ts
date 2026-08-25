import { parseMultipleChoice } from './chat.models';

describe('parseMultipleChoice', () => {
  it('reads the options out of a well-formed payload', () => {
    expect(parseMultipleChoice('{"options":["one","two"]}')).toEqual(['one', 'two']);
  });

  it('returns an empty list for a malformed payload rather than throwing', () => {
    // A renderer calls this from a template — a bad payload must degrade to "nothing to show".
    expect(parseMultipleChoice('not json')).toEqual([]);
    expect(parseMultipleChoice('')).toEqual([]);
    expect(parseMultipleChoice(undefined)).toEqual([]);
  });

  it('returns an empty list when options is missing or not an array', () => {
    expect(parseMultipleChoice('{}')).toEqual([]);
    expect(parseMultipleChoice('{"options":"one"}')).toEqual([]);
    expect(parseMultipleChoice('[]')).toEqual([]);
  });

  it('drops entries that are not usable option text', () => {
    expect(parseMultipleChoice('{"options":["one",null,42,"  ","two"]}')).toEqual(['one', 'two']);
  });
});
