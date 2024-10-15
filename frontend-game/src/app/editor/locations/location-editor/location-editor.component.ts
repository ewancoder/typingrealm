import { ChangeDetectionStrategy, Component, Input, OnDestroy, ViewEncapsulation } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { Location, LocationService } from '../location.service';

@Component({
    selector: 'tyr-location-editor',
    standalone: true,
    imports: [ReactiveFormsModule],
    templateUrl: './location-editor.component.html',
    styleUrl: './location-editor.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush,
    encapsulation: ViewEncapsulation.None
})
export class LocationEditorComponent implements OnDestroy {
    subscription: Subscription;
    @Input({ required: true }) locationId!: string;
    location: Location | undefined;
    updateLocationForm = new FormGroup({
        name: new FormControl('', Validators.required),
        description: new FormControl('', Validators.required),
        path: new FormControl('', Validators.required)
    });

    constructor(private locationService: LocationService) {
        this.subscription = locationService.locations$.subscribe(locations => {
            this.location = locations.find(x => x.id === this.locationId);
            if (this.location) {
                this.updateLocationForm.setValue({
                    name: this.location.name,
                    description: this.location.description,
                    path: this.location.path
                });
            }
        });
    }

    ngOnDestroy() {
        this.subscription.unsubscribe();
    }

    updateInfo() {
        this.locationService.updateLocation$(this.locationId, {
            name: this.updateLocationForm.value.name as string,
            description: this.updateLocationForm.value.description as string,
            path: this.updateLocationForm.value.path as string
        });
    }
}
