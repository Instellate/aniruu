// place files you want to import through the `$lib` alias in this folder.

import { ApiClient } from './client';
import { userStore } from './stores';

export const client = new ApiClient({
    BASE: 'http://localhost:5100'
});

userStore.subscribe((u) => {
    if (u) {
        client.request.config.TOKEN = `Bearer ${u.sessionToken}`;
    } else {
        client.request.config.TOKEN = undefined;
    }
});
