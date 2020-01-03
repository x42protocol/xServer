import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ColdStakingWithdrawConfirmationComponent } from './withdraw-confirmation.component';

describe('WithdrawConfirmationComponent', () => {
  let component: ColdStakingWithdrawConfirmationComponent;
  let fixture: ComponentFixture<ColdStakingWithdrawConfirmationComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ColdStakingWithdrawConfirmationComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ColdStakingWithdrawConfirmationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should be created', () => {
    expect(component).toBeTruthy();
  });
});
