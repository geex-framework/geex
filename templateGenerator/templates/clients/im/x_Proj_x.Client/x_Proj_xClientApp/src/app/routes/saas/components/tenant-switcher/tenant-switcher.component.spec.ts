import { ComponentFixture, TestBed } from "@angular/core/testing";

import { TenantSwitcherComponent } from "./tenant-switcher.component";

describe("TenantSwitcherComponent", () => {
  let component: TenantSwitcherComponent;
  let fixture: ComponentFixture<TenantSwitcherComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TenantSwitcherComponent],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TenantSwitcherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("should create", () => {
    expect(component).toBeTruthy();
  });
});
