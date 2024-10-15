import { AsyncPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Location, LocationService } from '../location.service';

@Component({
    selector: 'tyr-locations-editor',
    standalone: true,
    imports: [ReactiveFormsModule, AsyncPipe, RouterLink],
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
        this.locations$ = locationService.locations$;
    }

    onSubmit() {
        this.locationService
            .addLocation$({
                name: this.newLocationForm.value.name as string,
                description: this.newLocationForm.value.description as string,
                path: this.newLocationForm.value.name as string
            })
            .subscribe();

        this.newLocationForm.reset();
    }
}
