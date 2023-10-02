// place files you want to import through the `$lib` alias in this folder.

import { browser } from '$app/environment';
import { ApiClient, UserPermission } from './client';
import { userStore } from './stores';

export const client = new ApiClient({
    BASE: 'http://localhost:5100'
});

export function hasFlag(one: number, two: number) {
    return (one & two) !== 0;
}

userStore.subscribe((u) => {
    if (u) {
        client.request.config.TOKEN = u.sessionToken;
    } else {
        client.request.config.TOKEN = undefined;
    }
});

if (browser) {
    (async () => {
        const token = window.localStorage.getItem('token');
        if (token !== null) {
            client.request.config.TOKEN = token;
            const user = await client.user.userGetUserMe();

            userStore.set({
                sessionToken: token,
                permission: user.permission ?? (0 as UserPermission),
                id: user.id
            });
        }
    })();
}