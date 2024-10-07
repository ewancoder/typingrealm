import { Injectable } from '@angular/core';
import { initializeTypingState } from './typing';

@Injectable({
    providedIn: 'root'
})
export class TypingService {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    currentElement: any | undefined;
    currentKey: string | undefined;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    onScreenElements: Record<string, any> = {};

    pressKey(key: string) {
        const perf = performance.now();
        if (!this.currentElement) {
            this.currentElement = this.onScreenElements[key];
        }

        if (!this.currentElement) {
            return;
        }

        this.currentElement.pressKey(key, perf);
    }

    releaseKey(key: string) {
        const perf = performance.now();
        if (!this.currentElement) {
            return; // Only pressing the key should initiate element typing.
        }

        if (key === 'Escape') {
            this.resetCurrentElement();
            return;
        }

        this.currentElement.releaseKey(key, perf);
    }

    resetCurrentElement() {
        const text = this.currentElement.sourceText;
        this.currentElement.reset();
        this.currentElement.prepareText(text);
        this.currentElement = undefined;
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    addControl(element: HTMLDivElement, callback: (data: any) => void, length?: number): void {
        const text = this.textServiceGetText(Object.keys(this.onScreenElements), length);
        // Get text from text server, which will load a lot of texts async in background and just give out new texts sync.
        // However, if we need text synchronization between multiple players - there will be a separate async method (or signalr notification).

        const state = initializeTypingState(element, this.wrap(callback, element, text[0], length), false);
        state.prepareText(text);
        this.onScreenElements[text[0]] = state;
        console.log(Object.keys(this.onScreenElements));
    }

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    wrap(callback: (data: any) => void, element: HTMLDivElement, key: string, length?: number): (data: any) => void {
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        return (data: any) => {
            this.currentElement = undefined;
            delete this.onScreenElements[key];
            this.addControl(element, callback, length);

            callback(data);
        };
    }

    textServiceGetText(excludeChars: string[], length?: number) {
        if (length === 300) {
            //return 'The waves crashed against the shore, carrying with them whispers of a long-forgotten past. The seagulls cried out overhead, their haunting calls blending with the sound of the ocean. The salty air filled her lungs, invigorating her spirit as she walked along the sandy beach, lost in thought. Each st';
            return 'test';
        }

        for (const text of ['test one', 'another test', 'some longer text', 'very long and very difficult to type text']) {
            if (excludeChars.indexOf(text[0]) >= 0) {
                continue;
            }

            return text;
        }

        throw new Error('could not generate text');
    }
}
