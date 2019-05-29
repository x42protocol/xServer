import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CollateralSetupComponent } from './collateral-setup.component';

describe('CollateralSetup', () => {
  let component: CollateralSetupComponent;
  let fixture: ComponentFixture<CollateralSetupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [CollateralSetupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CollateralSetupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
