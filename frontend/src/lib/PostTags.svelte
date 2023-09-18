<script lang="ts">
    import type { PostTagsResponse } from './client';

    export let tags: PostTagsResponse[];
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
            <div class="bg-yellow-400 p-1 px-2 rounded-md">
                <span class="text-black text-sm">{tag}</span>
            </div>
        {/each}

        {#each character as tag}
            <div class="bg-green-400 p-1 px-2 rounded-md">
                <span class="text-black text-sm">{tag}</span>
            </div>
        {/each}

        {#each copyright as tag}
            <div class="bg-pink-400 p-1 px-2 rounded-md">
                <span class="text-black text-sm">{tag}</span>
            </div>
        {/each}

        {#each general as tag}
            <div class="bg-cyan-400 p-1 px-2 rounded-md">
                <span class="text-black text-sm">{tag}</span>
            </div>
        {/each}
    </div>
</div>
