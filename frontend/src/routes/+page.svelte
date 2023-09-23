<script lang="ts">
    import { client } from '$lib';
    import type { PageData } from './$types';

    export let data: PageData;
</script>

<svelte:head>
    <title>Posts</title>
</svelte:head>

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
