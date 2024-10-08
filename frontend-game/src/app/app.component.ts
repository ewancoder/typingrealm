import { NgClass } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NetworkWarningComponent } from './network-warning/network-warning.component';

import { AuthComponent } from './auth/auth.component';
import { AuthService } from './auth/auth.service';
import './auth/google-auth';

@Component({
    selector: 'tyr-root',
    standalone: true,
    imports: [RouterOutlet, NgClass, NetworkWarningComponent, AuthComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
    title = 'typingrealm';

    constructor(private auth: AuthService) {}

    async ngOnInit() {
        await this.auth.getToken();
    }
}
