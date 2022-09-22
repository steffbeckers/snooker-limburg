import { Component } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { environment } from '../environments/environment';
import { first } from 'rxjs';

@Component({
  selector: 'snooker-limburg-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent {
  constructor(public swPush: SwPush) {}

  async receiveNotifications(): Promise<void> {
    const sub: PushSubscription = await this.swPush.requestSubscription({ serverPublicKey: environment.publicKey })

    await fetch(environment.endpoint, {
      method: 'POST',
      body: JSON.stringify({ ...sub.toJSON(), enabled: true })
    });
  }

  async stopReceivingNotifiations(): Promise<void> {
    this.swPush.subscription
      .pipe(first())
      .subscribe(async (sub: PushSubscription | null) => {
      if (!sub) return;

      await this.swPush.unsubscribe();

      await fetch(environment.endpoint, {
        method: 'POST',
        body: JSON.stringify({ ...sub.toJSON(), enabled: false })
      })
    });
  }
}
