import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, switchMap, tap } from 'rxjs';
import { AuthService } from '../../auth/auth.service';

//const uri = 'https://dev.api.typingrealm.com/game/api/locations';
const uri = 'http://localhost:5001/api/editor/locations';

@Injectable({ providedIn: 'root' })
export class LocationService {
    private _locationsSubject: BehaviorSubject<Location[]> = new BehaviorSubject<Location[]>([]);
    locations$!: Observable<Location[]>;

    constructor(
        private http: HttpClient,
        private auth: AuthService
    ) {
        this.locations$ = this._locationsSubject.pipe(
            map(locations => locations.sort((left, right) => (left.name < right.name ? -1 : 1)))
        );
        this.getLocations$().subscribe(locations => {
            this._locationsSubject.next(locations);
        });
    }

    private getLocations$(): Observable<Location[]> {
        return this.auth.getToken$().pipe(
            switchMap(token =>
                this.http.get<Location[]>(uri, {
                    headers: {
                        Authorization: `Bearer ${token}`
                    }
                })
            )
        );
    }

    addLocation$(createLocation: CreateLocation): Observable<Location> {
        const body = createLocation;

        return this.auth.getToken$().pipe(
            switchMap(token =>
                this.http.post<Location>(uri, body, {
                    headers: {
                        Authorization: `Bearer ${token}`
                    }
                })
            ),
            tap(() => {
                this.getLocations$().subscribe(locations => {
                    this._locationsSubject.next(locations);
                });
            })
        );
    }

    updateLocation$(locationId: string, updateLocation: UpdateLocation) {
        return this.auth.getToken$().pipe(
            switchMap(token =>
                this.http.put<Location>(uri + '/' + locationId, updateLocation, {
                    headers: {
                        Authorization: `Bearer ${token}`
                    }
                })
            ),
            tap(() => {
                this.getLocations$().subscribe(locations => {
                    this._locationsSubject.next(locations);
                });
            })
        );
    }
}

export interface UpdateLocation {
    name: string;
    description: string;
    path: string;
}

export interface CreateLocation {
    name: string;
    description: string;
    path: string;
}

export interface WorldUnit {
    name: string;
    description: string;
    assets: Asset[];
}

export interface Location extends WorldUnit {
    id: string;
    path: string;
    paths: LocationPath[];
    inversePaths: LocationPath[];
}

export interface LocationPath extends WorldUnit {
    id: number;
    fromLocationId: string;
    toLocationId: string;
    distanceMarks: number;
}

export interface Asset {
    id: string;
    assetType: AssetType;
    data: Blob;
    path: string;
}

export enum AssetType {
    Unknown,
    Image
}
