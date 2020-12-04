import { Injectable, ChangeDetectorRef } from '@angular/core';

export interface NotificationTile {
  title: string;
  message: string;
  hint: string;
  icon: string;
  count?: number;
  data?: object;
  read?: boolean;
  date?: Date;
}

export interface NotificationMessage {
  title: string;
  body: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {

  /** List of active notifications. Use the add method to add new, do not push on this array. */
  public notifications: Array<NotificationTile>;

  // public count = 0;

  // public countUnread = 0;

  get any(): boolean {
    return (this.notifications.length > 0);
  }

  get anyUnread(): boolean {
    return (this.countUnread > 0);
  }

  get countUnread(): number {
    return this.notifications.filter(n => n.read !== true).length;
  }

  get count(): number {
    return this.notifications.length;
  }

  constructor() {
    this.notifications = new Array<NotificationTile>();

    // this.notifications.push(
    //     {
    //         title: 'Unable to connect with server',
    //         icon: 'warning',
    //         hint: 'This normally means there is communication issues between xCire, and the x42 Node background process.',
    //         message: 'Exception: STACK OVERFLOW!',
    //         count: 5
    //     });

    // this.notifications.push(
    //     {
    //         title: 'Unable to connect with server',
    //         icon: 'error',
    //         hint: 'This normally means there is communication issues between xCore, and the x42 Node background process.',
    //         message: 'Exception: STACK OVERFLOW!',
    //         count: 1
    //     });

    // this.notifications.push(
    //     {
    //         title: 'You sent a transaction',
    //         hint: '1230 coins',
    //         message: 'Is now fully confirmed (50 confirms).',
    //         icon: 'done_all',
    //     });

    // this.notifications.push(
    //     {
    //         title: 'You sent a transaction',
    //         hint: '2 coins',
    //         message: 'Unconfirmed',
    //         icon: 'send',
    //     });

    // this.notifications.push(
    //     {
    //         title: 'You made a block!',
    //         hint: 'You received staking rewards of 20 coins',
    //         message: 'You are currently 0.5% of the total network weight.',
    //         icon: 'plus_one',
    //     });

    // this.notifications.push(
    //     {
    //         title: 'You sent a transaction',
    //         hint: '2 coins',
    //         message: 'Unconfirmed',
    //         icon: 'done',
    //     });

    // this.notifications.push(
    //     {
    //         title: 'You add a new contact',
    //         hint: 'Contacts can be used to quickly send payments to merchants and people.',
    //         message: '',
    //         icon: 'bookmark',
    //     });
  }

  private find(tile: NotificationTile) {
    // tslint:disable-next-line: prefer-for-of
    for (let i = 0; i < this.notifications.length; ++i) {
      if (this.notifications[i].message === tile.message) {
        return this.notifications[i];
      }
    }
  }

  private sort() {
    this.notifications = this.notifications.sort((a, b) => {
      if (a.date > b.date) {
        return 1;
      }

      if (a.date < b.date) {
        return -1;
      }
    });
  }

  show(tile: NotificationMessage) {

    const notification = {
      title: tile.title,
      body: tile.body,
      icon: require('path').join(__dirname, '/assets/images/logo.png')
    };

    const nativeNotification = new window.Notification(notification.title, notification);

    nativeNotification.onclick = () => {
      console.log('Notification clicked');
    };
  }

  add(tile: NotificationTile) {

    const existing = this.find(tile);

    if (existing) {
      if (!existing.count) {
        existing.count = 1;
      }

      existing.count += 1;
      existing.date = new Date();
      this.sort();
      return;
    }

    if (!tile.date) {
      tile.date = new Date();
    }

    if (!tile.count) {
      tile.count = 1;
    }

    this.notifications.push(tile);

    // We only keep a certain list of notificatoins, so remove the oldest.
    if (this.notifications.length > 20) {
      this.notifications.shift();
    }

    this.sort();
  }

  read() {
    // tslint:disable-next-line: prefer-for-of
    for (let i = 0; i < this.notifications.length; ++i) {
      this.notifications[i].read = true;
    }
  }

  remove(tile: NotificationTile) {
    const index = this.notifications.findIndex(n => n === tile);
    this.notifications.splice(index, 1);
  }

  clear() {
    this.notifications = new Array<NotificationTile>();
  }
}
