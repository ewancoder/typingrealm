import { Component, ViewEncapsulation } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocationService } from '../location.service';

@Component({
    selector: 'tyr-locations-editor',
    standalone: true,
    imports: [ReactiveFormsModule],
    templateUrl: './locations-editor.component.html',
    styleUrl: './locations-editor.component.scss',
    encapsulation: ViewEncapsulation.None
})
export class LocationsEditorComponent {
    newLocationForm = new FormGroup({
        name: new FormControl('', Validators.required),
        description: new FormControl('', Validators.required)
    });

    constructor(private locationService: LocationService) {
        locationService.getLocations$().subscribe(x => console.log(x));
    }

    onSubmit() {
        console.log(this.newLocationForm.value);
    }
}
