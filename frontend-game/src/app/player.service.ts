import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PlayerService {
    getCurrentLocation(): Location {
        return {
            module: 'timberveil'
        };
    }
}

export interface Location {
    module: string;
}
