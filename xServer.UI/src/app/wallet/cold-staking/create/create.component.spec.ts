import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ColdStakingCreateComponent } from './create.component';

describe('ColdStakingCreateComponent', () => {
  let component: ColdStakingCreateComponent;
  let fixture: ComponentFixture<ColdStakingCreateComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ColdStakingCreateComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ColdStakingCreateComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
