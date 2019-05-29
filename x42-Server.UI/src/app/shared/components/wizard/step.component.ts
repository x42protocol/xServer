import {Component, Input} from '@angular/core';

@Component({
    selector: 'pe-step',
    styles: ['.pe-step-container {margin-bottom: 10px;}'],
    template: `
        <div *ngIf="active" [ngClass]="'ui-widget-content ui-corner-all pe-step-container'" [class]="styleClass">
            <ng-content></ng-content>
        </div>
    `
})
export class StepComponent {
    @Input() styleClass: string;
    @Input() label: string;
    active: boolean = false;
}
