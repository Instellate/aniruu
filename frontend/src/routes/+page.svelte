<script lang="ts">
    import { client } from '$lib';
    import { Paginator } from '@skeletonlabs/skeleton';
    import type { PageData } from './$types';
    import { env } from '$env/dynamic/public';
    import { ApiError } from '$lib/client';

    export let data: PageData;

    async function changePage(e: CustomEvent<number>) {
        const page = e.detail;
        try {
            const posts = await client.post.postGetPosts(page);
            data.posts = posts.posts;
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                // Empty
            }
        }
    }
</script>

<svelte:head>
    <title>Post | {env.PUBLIC_TITLE}</title>
    <meta property="og:title" content="Posts | {env.PUBLIC_TITLE}" />
    <meta property="og:type" content="website" />
</svelte:head>

<div class="flex flex-col justify-between h-full">
    <div class="flex flex-wrap gap-5 px-5 py-5">
        {#each data.posts as post}
            <a href="/post/{post.id}">
                <img
                    src="{client.request.config.BASE}{post.location}?size=320"
                    alt="A post"
                    class="max h-32 max-w-xz bg-white"
                />
            </a>
        {/each}
    </div>
    <div class="flex justify-center mb-4">
        <Paginator
            showNumerals
            showFirstLastButtons
            on:page={changePage}
            controlVariant="variant-ghost-surface"
            settings={{
                page: 1,
                limit: 7,
                size: data.total,
                amounts: []
            }}
        />
    </div>
</div>
