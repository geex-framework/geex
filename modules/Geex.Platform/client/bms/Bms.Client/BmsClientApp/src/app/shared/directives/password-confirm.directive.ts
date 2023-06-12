import { Directive, HostListener, Input, SimpleChanges, OnInit, OnChanges } from "@angular/core";
import { NG_VALIDATORS, Validator, AbstractControl, ValidationErrors, ValidatorFn } from "@angular/forms";
@Directive({
  selector: "[appForbiddenName]",
  providers: [{ provide: NG_VALIDATORS, useExisting: ForbiddenValidatorDirective, multi: true }],
})
export class ForbiddenValidatorDirective implements Validator {
  @Input("appForbiddenName") forbiddenName: string;

  validate(control: AbstractControl): ValidationErrors | null {
    return this.forbiddenName ? forbiddenNameValidator(this.forbiddenName)(control) : null;
  }
}
export function forbiddenNameValidator(newPassword: string): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } => {
    if (!control.value) {
      return { required: true };
    }
    return control.value !== newPassword ? { newPassword: true } : null;
  };
}
