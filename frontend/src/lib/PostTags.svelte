<script lang="ts">
    import type { Writable } from 'svelte/store';
    import type { PostTagsResponse } from './client';

    export let tags: PostTagsResponse[];
    export let editMode: Writable<boolean>;
    export let source: string | undefined;

    tags.sort((a, b) => a.name.localeCompare(b.name));

    const general: string[] = [];
    const character: string[] = [];
    const artist: string[] = [];
    const copyright: string[] = [];
    const meta: string[] = [];

    for (const tag of tags) {
        switch (tag.type) {
            case 0:
                general.push(tag.name);
                break;
            case 1:
                character.push(tag.name);
                break;
            case 2:
                artist.push(tag.name);
                break;
            case 3:
                copyright.push(tag.name);
                break;
            case 4:
                meta.push(tag.name);
                break;
        }
    }
</script>

<div class="pt-2 flex flex-col">
    <strong class="mb-2">Tags:</strong>
    <div class="flex flex-row flex-wrap gap-1.5">
        {#each artist as tag}
            <a
                href="/?tags={tag}"
                class="bg-yellow-400 p-1 px-2 rounded-md text-black text-sm"
            >
                {tag}
            </a>
        {/each}

        {#each character as tag}
            <a
                href="/?tags={tag}"
                class="bg-green-400 p-1 px-2 rounded-md text-black text-sm"
            >
                {tag}
            </a>
        {/each}

        {#each copyright as tag}
            <a
                href="/?tags={tag}"
                class="bg-pink-400 p-1 px-2 rounded-md text-black text-sm"
            >
                {tag}
            </a>
        {/each}

        {#each general as tag}
            <a
                href="/?tags={tag}"
                class="bg-cyan-400 p-1 px-2 rounded-md text-black text-sm"
            >
                {tag}
            </a>
        {/each}
    </div>

    {#if source}
        <a href={source} class="font-bold mt-4 my-2 text-blue-400">Source</a>
    {/if}

    <a href={source} class="font-bold my-2 text-blue-400">Download</a>

    <strong class="my-2">Actions:</strong>
    <div class="ml-2 text-blue-400">
        <button on:click={() => editMode.set(true)}>Edit</button>
    </div>
</div>
