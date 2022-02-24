import { Injectable } from '@angular/core';
import { UpdateInfo } from '../../shared/models/update-info';
import { NotificationService } from './notification.service';

@Injectable({
  providedIn: 'root'
})
export class UpdateService {

  static singletonInstance: UpdateService;
  private userNotifiedOfUpdate = false;
  public info: UpdateInfo;
  public progress: any;
  public downloaded = false;
  public available = false;
  public downloading = false;
  public LastUpdateCheck: Date;
  public IsChecking = false;

  constructor(
    private notificationService: NotificationService,
  ) {
    return UpdateService.singletonInstance;
  }

  checkForUpdate() {
    throw new Error('Method not implemented.');
  }

  downloadUpdate() {
    throw new Error('Method not implemented.');
  }

  installUpdate() {
    throw new Error('Method not implemented.');
  }
}
