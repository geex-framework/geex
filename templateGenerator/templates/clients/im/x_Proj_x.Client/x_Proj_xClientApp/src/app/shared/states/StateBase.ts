import { Injector } from "@angular/core";
import { Store } from "@ngxs/store";
import { Observable } from "rxjs";

export abstract class StateBase<TModel> {
  /**
   *
   */
  constructor(protected injector: Injector) {
    this.store = injector.get(Store);
  }
  protected store: Store;
  get snapshot(): TModel {
    throw new Error("invalid config for state");
  }
  get next(): Observable<TModel> {
    throw new Error("invalid config for state");
  }
  get observable(): Observable<TModel> {
    throw new Error("invalid config for state");
  }
}
