import { Component } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { environment } from '../environments/environment';

@Component({
  selector: 'snooker-limburg-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  constructor(public swPush: SwPush) {}

  async receiveNotifications(): Promise<void> {
    const sub: PushSubscription = await this.swPush.requestSubscription({ serverPublicKey: environment.publicKey })

    const response = await fetch(environment.endpoint, {
      method: 'POST',
      body: JSON.stringify(sub)
    })

    console.log(response);
  }

  stopReceivingNotifiations(): void {
    this.swPush.unsubscribe().then();
  }
}
