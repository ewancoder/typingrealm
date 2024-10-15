import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { Location, LocationService } from '../location.service';

@Component({
    selector: 'tyr-locations-editor',
    standalone: true,
    imports: [ReactiveFormsModule, AsyncPipe],
    templateUrl: './locations-editor.component.html',
    styleUrl: './locations-editor.component.scss',
    encapsulation: ViewEncapsulation.None,
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LocationsEditorComponent {
    locations$!: Observable<Location[]>;
    newLocationForm = new FormGroup({
        name: new FormControl('', Validators.required),
        description: new FormControl('', Validators.required)
    });

    constructor(private locationService: LocationService) {
        this.locations$ = locationService.getLocations$();
    }

    onSubmit() {
        console.log(this.newLocationForm.value);
    }
}
