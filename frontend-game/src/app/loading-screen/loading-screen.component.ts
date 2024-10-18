import { AsyncPipe, NgClass } from '@angular/common';
import { ChangeDetectionStrategy, Component, Injectable, Input, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Observable } from 'rxjs';
import { ResourceLoadService } from '../resource-load.service';

@Component({
    selector: 'tyr-loading-screen',
    standalone: true,
    imports: [NgClass, AsyncPipe],
    templateUrl: './loading-screen.component.html',
    styleUrl: './loading-screen.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingScreenComponent implements OnInit {
    @Input({ required: true }) module!: string;
    resourcesLoading!: Observable<number>;
    accepted = false;

    constructor(
        private router: Router,
        private load: ResourceLoadService,
        public hd: HdService
    ) {}

    ngOnInit() {
        this.resourcesLoading = this.load
            .loadResources(this.module)
            .pipe(finalize(() => this.router.navigate([this.getRoute()])));
    }

    private getRoute(): string {
        if (this.module === 'menu') return '/menu';

        return '/game';
    }
}

@Injectable({ providedIn: 'root' })
export class HdService {
    get enabled() {
        return localStorage.getItem('hd') === 'true';
    }

    enable() {
        localStorage.setItem('hd', 'true');
    }

    disable() {
        localStorage.setItem('hd', 'false');
    }
}
