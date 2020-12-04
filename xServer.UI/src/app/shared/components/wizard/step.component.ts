import {Component, Input} from '@angular/core';

@Component({
    selector: 'app-pe-step',
    styles: ['.pe-step-container {margin-bottom: 10px;}'],
    template: `
        <div *ngIf="active" [ngClass]="'p-widget-content p-corner-all pe-step-container'" [class]="styleClass">
            <ng-content></ng-content>
        </div>
    `
})
export class StepComponent {
    @Input() styleClass: string;
    @Input() label: string;
    active = false;
}
