import { Component, OnInit } from '@angular/core';
import { ApplicationStateService } from '../../../../shared/services/application-state.service';
import { UpdateService } from '../../../../shared/services/update.service';

@Component({
  selector: 'app-about',
  templateUrl: './about.component.html',
  styleUrls: ['./about.component.css']
})
export class AboutComponent implements OnInit {
  constructor(
    public appState: ApplicationStateService,
    public updateService: UpdateService,
  ) { }

  public installEnabled: boolean;

  ngOnInit() {
    throw new Error('Method not implemented.');
  }

  openWalletDirectory(directory: string): void {
    throw new Error('Method not implemented.');
  }

  releaseDateFormatted() {
    const updatedTime = new Date(this.updateService.info.releaseDate);
    return updatedTime.toLocaleDateString();
  }

  lastCheckDateFormatted() {
    const lastCheckedTime = new Date(this.updateService.LastUpdateCheck);
    return lastCheckedTime.toLocaleString();
  }

  releaseNotesFormatted() {
    console.log(this.updateService.info.releaseNotes.replace(/<[^>]*>?/gm, ''));
    this.updateService.info.releaseNotes.replace(/<[^>]*>?/gm, '');
  }
}
