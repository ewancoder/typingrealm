// Copied from frontend project. Do not modify here.

// eslint-disable-next-line @typescript-eslint/no-explicit-any
declare const google: any;

import { setupAuth } from './auth.service';

let initializedPromiseResolve: (() => void) | undefined = undefined;
const initializedPromise = new Promise<void>(resolve => (initializedPromiseResolve = resolve));

export function initializeGoogleAuth(authElement: HTMLDivElement) {
    google.accounts.id.initialize({
        client_id: '400839590162-24pngke3ov8rbi2f3forabpaufaosldg.apps.googleusercontent.com',
        context: 'signin',
        ux_mode: 'popup',
        callback: authCallback,
        auto_select: true,
        itp_support: true,
        use_fedcm_for_prompt: true,
        cancel_on_tap_outside: false
    });

    google.accounts.id.renderButton(authElement, {
        type: 'standard',
        shape: 'rectangular',
        theme: 'filled_black',
        text: 'signin',
        size: 'large',
        logo_alignment: 'left'
    });

    // Notify that google library is initialized.
    if (initializedPromiseResolve) initializedPromiseResolve();
}

setupAuth(getToken);

let acquiredTokenResolves: ((token: string) => void)[] = [];
async function getToken(): Promise<string> {
    await initializedPromise; // Wait until google library is initialized.

    // Show FedCM / OneTap.
    google.accounts.id.prompt();

    return await new Promise<string>(resolve => {
        acquiredTokenResolves.push(resolve);
    });
}

async function authCallback(response: AuthResponse) {
    const acquiredTokenResolvesCopy = acquiredTokenResolves;
    acquiredTokenResolves = [];
    const token = response.credential;
    for (const resolve of acquiredTokenResolvesCopy) {
        // Resolve the same for anyone who used getToken() method and is waiting for it.
        resolve(token);
    }
}

interface AuthResponse {
    credential: string;
}
