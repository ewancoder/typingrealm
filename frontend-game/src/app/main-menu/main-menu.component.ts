import { NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HdService } from '../loading-screen/loading-screen.component';
import { PlayerService } from '../player.service';
import { initializeTypingState } from '../typing';

@Component({
    selector: 'tyr-main-menu',
    standalone: true,
    imports: [NgClass],
    templateUrl: './main-menu.component.html',
    styleUrl: './main-menu.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class MainMenuComponent implements OnInit, OnDestroy {
    constructor(
        public hd: HdService,
        private router: Router,
        private playerService: PlayerService
    ) {}

    press!: (e: KeyboardEvent) => void;
    release!: (e: KeyboardEvent) => void;

    // TODO: Print INTRO text in the menu with typer replay.
    ngOnInit() {
        const element = document.getElementById('start-game');
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        const typingState = initializeTypingState(element, (data: any) => {
            // TODO: Always upload typed data for further analysis.
            console.log('TODO: should upload', data);
            const location = this.playerService.getCurrentLocation();
            this.router.navigate([`/loading/${location.module}`]);
        });

        typingState.prepareText('Enter the game');

        this.press = (e: KeyboardEvent) => typingState.pressKey(e.key);
        this.release = (e: KeyboardEvent) => typingState.releaseKey(e.key);

        document.addEventListener('keydown', this.press);
        document.addEventListener('keyup', this.release);
    }

    ngOnDestroy() {
        document.removeEventListener('keydown', this.press);
        document.removeEventListener('keyup', this.release);
    }
}
