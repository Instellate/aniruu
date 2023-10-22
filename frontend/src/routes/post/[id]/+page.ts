import { client } from '$lib';
import { error } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { ApiError } from '$lib/client';

export const load = (async ({ params, url }) => {
    const num = Number(params.id);
    if (Number.isNaN(num)) {
        throw error(404, {
            message: 'notFound'
        });
    }

    try {
        const pageStr = url.searchParams.get('commentPage');
        const page = pageStr ? Number(pageStr) : 1;

        const post = await client.post.postGetPost(num);
        const commentPage = await client.post.postGetComments(num, page);

        return {
            post: post,
            commentPage: commentPage,
            currentPage: page
        };
    } catch (e: unknown) {
        if (e instanceof ApiError) {
            if (e.status === 404) {
                throw error(404, {
                    message: 'notFound'
                });
            }
        }
        throw error(500, {
            message: 'internal'
        });
    }
}) satisfies PageLoad;
