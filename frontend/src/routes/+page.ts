import { client } from '$lib';
import type { PageLoad } from './$types';

export const load = (async ({ url }) => {
    const tags = url.searchParams.getAll("tags");
    let page = Number(url.searchParams.get("page")) === 0 ? 1 : Number(url.searchParams.get("page"));
    page = Number.isNaN(page) ? 1 : page;

    const response = await client.post.postGetPosts(page, tags);

    return {
        posts: response
    };
}) satisfies PageLoad;