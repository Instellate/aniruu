import { client } from '$lib';
import { error } from '@sveltejs/kit';
import type { PageLoad } from './$types';
import { ApiError, type PostResponse } from '$lib/client';

export const load = (async ({ params }) => {
    const num = Number(params.id);
    if (Number.isNaN(num)) {
        throw error(404, {
            message: 'notFound'
        });
    }

    try {
        const post = await client.post.postGetImageData(num);

        return {
            post: post
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
        })
    }
}) satisfies PageLoad;