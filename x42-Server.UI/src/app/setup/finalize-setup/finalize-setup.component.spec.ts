import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FinalizeSetupComponent } from './finalize-setup.component';

describe('FinalizeSetup', () => {
  let component: FinalizeSetupComponent;
  let fixture: ComponentFixture<FinalizeSetupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [FinalizeSetupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FinalizeSetupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
