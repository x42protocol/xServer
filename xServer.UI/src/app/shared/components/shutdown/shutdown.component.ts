import { Component } from '@angular/core';
import { ApplicationStateService } from '../../../shared/services/application-state.service';
import { ThemeService } from '../../services/theme.service';

@Component({
  selector: 'app-shutdown',
  templateUrl: './shutdown.component.html',
  styleUrls: ['./shutdown.component.css']
})
export class ShutdownComponent {
  constructor(
    public appState: ApplicationStateService,
    private themeService: ThemeService,
  ) {
    this.themeService.setTheme();
  }

  forceExit() {
    window.close();
  }

}
