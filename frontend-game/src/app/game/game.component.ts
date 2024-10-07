import { AfterViewInit, Component, ElementRef, OnDestroy, Renderer2, signal, ViewChild } from '@angular/core';
import { ResourceLoadService } from '../resource-load.service';
import { TypingService } from '../typing.service';

@Component({
    selector: 'tyr-game',
    standalone: true,
    imports: [],
    templateUrl: './game.component.html',
    styleUrl: './game.component.scss'
})
export class GameComponent implements AfterViewInit, OnDestroy {
    @ViewChild('locationPicture') locationPicture!: ElementRef<HTMLDivElement>;
    public woodSignal = signal(0);
    public axeCondition = signal(100);
    public level = signal(1);
    public experience = signal(0);

    constructor(
        private renderer: Renderer2,
        private resources: ResourceLoadService,
        private typingService: TypingService
    ) {}

    press!: (e: KeyboardEvent) => void;
    release!: (e: KeyboardEvent) => void;

    ngAfterViewInit() {
        const blobUrl = this.resources.loadedResources['assets/world/timberveil/veilwood/lumberyard/yard1.png'];
        this.renderer.setStyle(this.locationPicture.nativeElement, 'background-image', `url('${blobUrl}')`);

        {
            const element = document.createElement('div');
            element.classList.add('tyr-typing');
            this.locationPicture.nativeElement.appendChild(element);
            this.typingService.addControl(
                element,
                () => {
                    // Get both values from the server to update.
                    this.woodSignal.set(this.woodSignal() + 2);
                    this.axeCondition.set(this.axeCondition() - 5);
                    this.experience.set(this.experience() + 5);
                    if (this.experience() > 100) {
                        this.level.set(this.level() + 1);
                        this.experience.set(this.experience() - 100);
                    }
                },
                300
            );
        }

        this.press = (e: KeyboardEvent) => this.typingService.pressKey(e.key);
        this.release = (e: KeyboardEvent) => this.typingService.releaseKey(e.key);
        document.addEventListener('keydown', this.press);
        document.addEventListener('keyup', this.release);
    }

    ngOnDestroy() {
        document.removeEventListener('keydown', this.press);
        document.removeEventListener('keyup', this.release);
    }
}
