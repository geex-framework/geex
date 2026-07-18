import { Component } from "@angular/core";
import { RouterLink } from "@angular/router";
import { CommonModule } from "@angular/common";

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div style="padding:16px;font-family:sans-serif">
      <h2>Geex Mocking</h2>
      <p>SuperAdmin-only management console for local external dependency simulation.</p>
      <ul>
        <li><a routerLink="/mocking/wechat">WeChat profiles</a></li>
        <li><a routerLink="/mocking/sms">SMS inbox</a></li>
        <li><a routerLink="/mocking/payments">Payments</a></li>
      </ul>
    </div>
  `,
})
export class MockingHomePage {}
