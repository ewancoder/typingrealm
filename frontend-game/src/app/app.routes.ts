import { Injectable } from '@angular/core';
import { CanActivate, Router, Routes } from '@angular/router';
import { LocationsEditorComponent as LocationsEditorComponent } from './editor/locations/locations-editor/locations-editor.component';
import { GameComponent } from './game/game.component';
import { LoadingScreenComponent } from './loading-screen/loading-screen.component';
import { MainMenuComponent } from './main-menu/main-menu.component';
import { NetworkWarningComponent } from './network-warning/network-warning.component';
import { PlayerService } from './player.service';
import { ResourceLoadService } from './resource-load.service';

@Injectable({
    providedIn: 'root'
})
export class ResourcesLoadedGuard implements CanActivate {
    constructor(
        private resourceLoadService: ResourceLoadService,
        private router: Router,
        private playerService: PlayerService
    ) {}

    canActivate(): boolean {
        const module = this.playerService.getCurrentLocation().module;
        if (this.resourceLoadService.loadedModules.indexOf(module) >= 0) {
            return true;
        } else {
            // Simple implementation. In future should pass here specific module to load.
            this.router.navigate([`/loading/${module}`]);
            return false; // Prevent access to the route
        }
    }
}

export const routes: Routes = [
    { path: '', redirectTo: '/warning', pathMatch: 'full' },
    { path: 'warning', component: NetworkWarningComponent },
    { path: 'loading/:module', component: LoadingScreenComponent },
    { path: 'menu', component: MainMenuComponent },
    { path: 'game', component: GameComponent, canActivate: [ResourcesLoadedGuard] },
    { path: 'editor/locations', component: LocationsEditorComponent }
];
