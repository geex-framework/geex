export async function resolve(specifier, context, nextResolve) {
  if (specifier === "@geexcode/geex-angular") {
    return { url: "data:text/javascript,export const GEEX_I18N = 'GEEX_I18N'; export const provideGeexModuleContribution = value => value;", shortCircuit: true };
  }
  return nextResolve(specifier, context);
}
