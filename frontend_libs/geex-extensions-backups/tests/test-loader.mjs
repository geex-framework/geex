// The headless factory does not need Geex's browser UI bootstrap in Node tests.
export async function resolve(specifier, context, nextResolve) {
  if (specifier === "@geexcode/geex-angular") {
    return { url: "data:text/javascript,export const provideGeexModuleContribution = value => value;", shortCircuit: true };
  }
  return nextResolve(specifier, context);
}
