import { Component } from '@angular/core';
import { LocationService } from '../location.service';

@Component({
    selector: 'tyr-locations-editor',
    standalone: true,
    imports: [],
    templateUrl: './locations-editor.component.html',
    styleUrl: './locations-editor.component.scss'
})
export class LocationsEditorComponent {
    constructor(private locationService: LocationService) {
        locationService.getLocations$().subscribe(x => console.log(x));
    }
}
