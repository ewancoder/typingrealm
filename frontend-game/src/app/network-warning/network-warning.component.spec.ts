import { ComponentFixture, TestBed } from '@angular/core/testing';

import { provideExperimentalZonelessChangeDetection } from '@angular/core';
import { tryInit } from '../test-init';
import { NetworkWarningComponent } from './network-warning.component';

describe('NetworkWarningComponent', () => {
    let component: NetworkWarningComponent;
    let fixture: ComponentFixture<NetworkWarningComponent>;

    beforeEach(async () => {
        tryInit();
        await TestBed.configureTestingModule({
            providers: [provideExperimentalZonelessChangeDetection()],
            imports: [NetworkWarningComponent]
        }).compileComponents();

        fixture = TestBed.createComponent(NetworkWarningComponent);
        component = fixture.componentInstance;
    });

    describe('initial test', () => {
        it('should initialize', () => expect(component).toBeDefined());
    });
});
