import { FormControl, FormGroup } from "@angular/forms";

/** Loose entity / DTO shape used by page bases (host `Hint<T>`-compatible). */
export type GeexHint<T> = T & Record<string, any>;

/**
 * Host-compatible typed FormGroup (mirrors admin `TypedFormGroup` augmentation).
 * Keeps `controls.*` as FormControl for template `[formControl]` bindings.
 */
export type GeexTypedFormGroup<TValue> = FormGroup & {
  controls: { [K in keyof TValue]: FormControl<any> };
  value: TValue;
};
