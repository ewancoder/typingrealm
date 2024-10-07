import { NgClass } from '@angular/common';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NetworkWarningComponent } from './network-warning/network-warning.component';

@Component({
    selector: 'tyr-root',
    standalone: true,
    imports: [RouterOutlet, NgClass, NetworkWarningComponent],
    templateUrl: './app.component.html',
    styleUrl: './app.component.scss'
})
export class AppComponent {
    title = 'typingrealm';
}
