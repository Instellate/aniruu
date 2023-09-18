<script lang="ts">
    import { client } from '$lib';
    import PostTags from '$lib/PostTags.svelte';
    import { preference, setSidebarContent } from '$lib/stores';
    import { writable } from 'svelte/store';
    import type { PageData } from './$types';
    import type { TagType } from '$lib/client';
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';

    export let data: PageData;

    const store = writable<boolean>(false);

    setSidebarContent({
        component: PostTags,
        data: {
            editMode: store,
            tags: data.post.tags,
            source: data.post.source
        }
    });

    function tagTypeToString(type: TagType): string {
        switch (type) {
            case 0:
                return 'general';
            case 1:
                return 'character';
            case 2:
                return 'artist';
            case 3:
                return 'copyright';
            case 4:
                return 'meta';
            default:
                return '';
        }
    }

    function tagsToString(): string {
        data.post.tags.sort((t) => t.type);
        return data.post.tags
            .map((t) => {
                const typeString = tagTypeToString(t.type);
                if (typeString === 'general') {
                    return t.name;
                } else {
                    return `${typeString}:${t.name}`;
                }
            })
            .join(' ');
    }

    let editValue: string = tagsToString();
    async function updatePost() {
        try {
            await client.post.postEditPost(data.post.id, {
                tags: editValue
            });
            await goto($page.url);
        } catch (err: unknown) {
            /* empty */
        }
    }

    let imgUrl = $preference.prefersOriginalSize
        ? `${client.request.config.BASE + data.post.location}`
        : `${client.request.config.BASE + data.post.location}?size=720`;

    function changeImgSize() {
        imgUrl = imgUrl.endsWith('720')
            ? `${client.request.config.BASE + data.post.location}`
            : `${client.request.config.BASE + data.post.location}?size=720`;
    }
</script>

<svelte:head>
    <title>Post {data.post.id}</title>
</svelte:head>

<div class="px-5 py-5 flex flex-col">
    <button on:click={changeImgSize}>
        <img src={imgUrl} alt="Post" class="bg-white" />
    </button>

    {#if $store}
        <div class="mt-2 flex flex-col gap-2">
            <textarea
                placeholder="Tags..."
                rows="4"
                cols="50"
                class="textarea w-fit"
                bind:value={editValue}
            />
            <button class="btn variant-ghost-surface w-fit" on:click={updatePost}
                >Submit</button
            >
        </div>
    {/if}
</div>
