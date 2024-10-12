// Copied from frontend project. Do not modify here.

import { Injectable, signal } from '@angular/core';
import { from, Observable } from 'rxjs';
import { createLock } from './lock.js';

let _getToken: (() => Promise<string>) | undefined = undefined;

/** Sets up the function that is getting the token from authentication provider.
 * This function will be called every time when we need a fresh token, which
 * is then being cached until its expiration time. */
export function setupAuth(getToken: () => Promise<string>) {
    _getToken = getToken;
}

/** Use getToken method to get token, and isLoggedIn field to check whether
 * user is logged in. */
@Injectable({ providedIn: 'root' })
export class AuthService {
    private token: string | undefined = undefined;
    private lock = createLock();
    public needsAuthSignal = signal(true);

    getToken$(): Observable<string> {
        return from(this.getToken());
    }

    /** Gets authentication token either from cache or from authentication
     * provider, and then caches it. */
    async getToken(): Promise<string> {
        if (this.token && !this.needsAuthSignal()) {
            return this.token;
        } else {
            // Make sure this function actually gets the token only once,
            // even if called multiple times in a row.
            await this.lock.wait();

            try {
                if (this.token && !this.needsAuthSignal()) {
                    return this.token;
                }

                if (!_getToken) throw new Error('Authentication is not initialized.');

                this.token = await _getToken(); // Get token using authentication provider.
                this.needsAuthSignal.set(false);
                console.log('Authentication successful.');

                setTimeout(() => {
                    this.needsAuthSignal.set(true); // Reset authenticated state when token is about to expire.
                }, this.getMsTillAuthenticationIsRequired(this.token));

                return this.token;
            } finally {
                this.lock.release();
            }
        }
    }

    // Functions below calculate the time when token expires.

    private getMsTillAuthenticationIsRequired(token: string) {
        return this.getExpiration(token) * 1000 - Date.now() - 60 * 5 * 1000;
    }

    private getExpiration(token: string) {
        return this.parseJwt(token).exp;
    }

    private parseJwt(token: string) {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            window
                .atob(base64)
                .split('')
                .map(function (c) {
                    return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
                })
                .join('')
        );

        return JSON.parse(jsonPayload);
    }
}
