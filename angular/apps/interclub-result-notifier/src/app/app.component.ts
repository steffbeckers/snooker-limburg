import { Component } from '@angular/core';
import { SwPush } from '@angular/service-worker';

@Component({
  selector: 'snooker-limburg-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  constructor(public swPush: SwPush) {}

  receiveNotifications(): void {
    this.swPush.requestSubscription({ serverPublicKey: 'BP4eujZym5wbx93H0W9jfIImBNun0GNZPA0ytd2wTeldb7cgHKYvNa1NRtJdAJ8d3e3JYojXug7AXVVh89q1HYg' })
      .then((sub: PushSubscription) => console.log("PushSubscription", sub));
  }

  stopReceivingNotifiations(): void {
    this.swPush.unsubscribe().then();
  }
}
