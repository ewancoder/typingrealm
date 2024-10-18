import { Component, ElementRef, HostListener, ViewChild } from '@angular/core';

import { AuthService } from './auth.service';
import { initializeGoogleAuth } from './google-auth';

@Component({
    selector: 'tyr-auth',
    standalone: true,
    template: '@if (auth.needsAuthSignal()) { <div class="auth"><div>Please sign in</div><div #authentication></div></div> }',
    styles: '.hidden { display: none; } :host { display: flex; justify-content: center; } .auth { margin: 10px; padding: 20px; background-color: rgba(60, 60, 60, 0.8); z-index: 90000; position: absolute; display: flex; justify-content: center; gap: 20px; }'
})
export class AuthComponent {
    @ViewChild('authentication') authElement!: ElementRef<HTMLDivElement>;
    @HostListener('window:load')
    onLoad() {
        console.log(this.authElement);
        initializeGoogleAuth(this.authElement.nativeElement);
    }

    constructor(public auth: AuthService) {}
}
