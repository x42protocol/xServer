import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ColdStakingCreateHotComponent } from './create-hot.component';

describe('ColdStakingCreateHotComponent', () => {
  let component: ColdStakingCreateHotComponent;
  let fixture: ComponentFixture<ColdStakingCreateHotComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ColdStakingCreateHotComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ColdStakingCreateHotComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
