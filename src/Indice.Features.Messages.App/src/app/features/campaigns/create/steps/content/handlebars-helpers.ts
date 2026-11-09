import Handlebars from 'handlebars';

/**
 * Registers the custom helpers available to message templates on the given Handlebars env.
 * Mirrors `MessageHandlebarsHelpers` in Indice.Features.Messages.Core, so the preview renders like a sent message;
 * keep both in sync.
 *
 * - `concat`: joins its arguments into a single string, e.g. `(concat data.baseUrl "/documents")`. Missing values render as empty.
 */
export function registerMessageHelpers(env: typeof Handlebars): typeof Handlebars {
  // The last argument is the Handlebars options object, not a value.
  env.registerHelper('concat', (...args: unknown[]) => args.slice(0, -1).map(formatArgument).join(''));
  return env;
}

function formatArgument(value: unknown): string {
  if (value === null || value === undefined) {
    return '';
  }
  return typeof value === 'object' ? JSON.stringify(value) : String(value);
}
