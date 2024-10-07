import { Injectable } from '@angular/core';
import { combineLatest, distinctUntilChanged, finalize, map, Observable, of, share, startWith, Subject } from 'rxjs';
import { HdService } from './loading-screen/loading-screen.component';

@Injectable({
    providedIn: 'root'
})
export class ResourceLoadService {
    readonly timberveil = 'timberveil';
    readonly menu = 'menu';
    loadedResources: Record<string, string> = {};

    public loadedModules: string[] = [];

    constructor(private hd: HdService) {}

    loadResources(worldPart: string): Observable<number> {
        // Simple implementation. In future should check whether a specific module has been loaded completely.
        this.loadedModules.push(worldPart);

        if (!this.hd.enabled) {
            console.log('HD is disabled, skipping loading images.');
            return of(100);
        }

        let urls: string[] = [];
        if (worldPart === this.timberveil) {
            const part = this.getResources('world/timberveil');
            if (part) urls = this.getAssets(part, 'assets/world');
        }

        if (worldPart === this.menu) {
            const part = this.getResources('menu');
            if (part) urls = this.getAssets(part, 'assets');
        }

        const count = urls.length;
        const observables = urls.map(url => {
            const observable$ = this.loadResource(url).pipe(
                startWith({ progress: 0, total: 100 }),
                finalize(() => {
                    console.log(`Loaded resource: ${url}`);
                })
            );

            return observable$;
        });

        const progress$ = combineLatest(observables).pipe(
            map(values => {
                const allProgresses = values
                    .map(progress => (progress.progress * 100) / progress.total)
                    .reduce((sum, currentValue) => sum + currentValue, 0);

                const totalProgress = allProgresses / count;

                // 4000 pixels for 4k resolution, 4000 positions.
                return Math.floor(40 * totalProgress) / 40;
            }),
            distinctUntilChanged(),
            share()
        );

        return progress$;
    }

    private getAssets(resources: Resources, prefix: string): string[] {
        const assets: string[] = [];

        this.processResources(`${prefix}/${resources.name}`, assets, resources);

        return assets;
    }

    private processResources(prefix: string, assets: string[], resources: Resources) {
        if (resources.assets) {
            for (const asset of resources.assets) {
                assets.push(`${prefix}/${asset}`);
            }
        }

        if (resources.subFolders) {
            for (const subFolder of resources.subFolders) {
                this.processResources(`${prefix}/${subFolder.name}`, assets, subFolder);
            }
        }
    }

    private getResources(path: string): Resources | null {
        const parts = path.split('/');
        let resource: Resources | undefined = resources;

        for (const part of parts) {
            resource = resource.subFolders?.find(s => s.name === part);
            if (!resource) return null;
        }

        return resource;
    }

    private loadResource(url: string): Observable<Progress> {
        const subject = new Subject<Progress>();
        if (this.loadedResources[url]) {
            subject.next({ progress: 100, total: 100 });
            subject.complete();
            return subject.asObservable();
        }

        const request = indexedDB.open('resourcesDB', 1);
        request.onupgradeneeded = (event: IDBVersionChangeEvent) => {
            const db = (event.target as IDBOpenDBRequest).result;
            db.createObjectStore('resources', { keyPath: 'url' });
        };

        request.onsuccess = (event: Event) => {
            const db = (event.target as IDBOpenDBRequest).result;
            const transaction = db.transaction(['resources'], 'readonly');
            const store = transaction.objectStore('resources');

            const getRequest = store.get(url);
            getRequest.onsuccess = event => {
                const result = (event.target as IDBRequest<Event>).result;
                if (result && 'blob' in result && result.blob instanceof Blob) {
                    const blobUrl = URL.createObjectURL(result.blob);
                    this.loadedResources[url] = blobUrl;
                    subject.next({ progress: 100, total: 100 });
                    subject.complete();
                    console.log(`got item from the database: ${url}`);
                } else {
                    console.log(`image not found in database: ${url}`);
                    this.downloadImage(url, subject);
                }
            };
        };

        request.onerror = event => {
            console.error('Database error:', (event.target as IDBOpenDBRequest).error);
        };

        return subject.asObservable();
    }

    private downloadImage(url: string, subject: Subject<Progress>) {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', url, true);
        xhr.responseType = 'blob';
        xhr.onload = () => {
            this.storeImage(url, xhr.response);
            this.loadedResources[url] = URL.createObjectURL(xhr.response);
            subject.complete();
        };
        xhr.onprogress = function (event) {
            subject.next({
                progress: event.loaded,
                total: event.total
            });
        };
        xhr.onerror = function () {
            subject.error('Failed to download the resource.');
        };
        xhr.send();
    }

    private storeImage(url: string, blob: Blob): void {
        const request = indexedDB.open('resourcesDB', 1);

        request.onsuccess = (event: Event) => {
            const db = (event.target as IDBOpenDBRequest).result;
            const transaction = db.transaction(['resources'], 'readwrite');
            const store = transaction.objectStore('resources');

            const item: DatabaseResource = { url: url, blob: blob };
            store.put(item);
            console.log(`stored item in the database: ${url}`);
        };

        request.onerror = event => {
            console.error('Database error:', (event.target as IDBOpenDBRequest).error);
        };
    }
}

interface DatabaseResource {
    url: string;
    blob: Blob;
}

interface Progress {
    progress: number;
    total: number;
}

const resources: Resources = {
    name: 'assets',
    subFolders: [
        {
            name: 'world',
            subFolders: [
                {
                    name: 'timberveil',
                    subFolders: [
                        {
                            name: 'veilwood',
                            subFolders: [
                                {
                                    name: 'lumberyard',
                                    assets: ['yard1.png']
                                }
                            ]
                        }
                    ]
                }
            ]
        },
        {
            name: 'menu',
            assets: ['background.png', 'caption.png']
        }
    ]
};

interface Resources {
    name: string;
    assets?: string[];
    subFolders?: Resources[];
}
