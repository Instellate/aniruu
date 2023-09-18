import type { PageLoad } from './$types';

export const load = (async () => {
    return {
        text: 'Hello there!'
    };
}) satisfies PageLoad;
