import { Injectable } from "@angular/core";
import { BehaviorSubject } from "rxjs";

export type SubscriptionConnectionState = "connecting" | "connected" | "disconnected";
@Injectable({ providedIn: "root" })
export class GeexSubscriptionConnection {
  private readonly state = new BehaviorSubject<SubscriptionConnectionState>("disconnected");
  readonly changes = this.state.asObservable();
  update(state: SubscriptionConnectionState): void { this.state.next(state); }
}
