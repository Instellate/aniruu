<script lang="ts">
    import { client } from '$lib';
    import { Paginator } from '@skeletonlabs/skeleton';
    import type { PageData } from './$types';

    export let data: PageData;

    async function changePage(e: CustomEvent<number>) {
        const page = e.detail;
    }
</script>

<svelte:head>
    <title>Posts</title>
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