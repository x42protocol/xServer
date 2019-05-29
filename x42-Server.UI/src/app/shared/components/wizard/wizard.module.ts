import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StepComponent } from './step.component';
import { StepsComponent } from './steps.component';

// PrimeNG Components.
import { ButtonModule } from 'primeng/button';
import { StepsModule } from 'primeng/steps';

@NgModule({
  imports: [CommonModule, ButtonModule, StepsModule],
  exports: [StepComponent, StepsComponent],
  declarations: [StepComponent, StepsComponent]
})
export class WizardModule {
}
