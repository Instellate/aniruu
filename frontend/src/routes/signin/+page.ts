import { client } from '$lib';
import type { PageLoad } from './$types';

export const load = (async () => {
    const response = await client.account.accountGetAuthUri();

    return {
        uris: response
    };
}) satisfies PageLoad;
