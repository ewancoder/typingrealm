import { Component, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { HdService } from '../loading-screen/loading-screen.component';

@Component({
    selector: 'tyr-network-warning',
    standalone: true,
    imports: [],
    templateUrl: './network-warning.component.html',
    styleUrl: './network-warning.component.scss'
})
export class NetworkWarningComponent {
    constructor(
        private router: Router,
        private hd: HdService
    ) {}

    @HostListener('window:keyup.enter')
    onEnter() {
        this.hd.enable();
        this.router.navigate(['/loading/menu']);
    }

    @HostListener('window:keyup.space')
    onSpace() {
        this.hd.disable();
        this.router.navigate(['/loading/menu']);
    }
}
