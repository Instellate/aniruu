<script lang="ts">
    import { client } from '$lib';
    import PostTags from '$lib/PostTags.svelte';
    import { preference, setSidebarContent } from '$lib/stores';
    import { writable } from 'svelte/store';
    import type { PageData } from './$types';
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';
    import { ApiError, type TagType } from '$lib/client';
    import { type ToastSettings, getToastStore } from '@skeletonlabs/skeleton';

    export let data: PageData;

    const store = writable<boolean>(false);

    async function deletePost() {
        try {
            await client.post.postDeletePost(data.post.id);
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                if (err.status === 403) {
                    const t: ToastSettings = {
                        message: "You can't delete this post",
                        background: 'variant-filled-error'
                    };
                    getToastStore().trigger(t);
                } else if (err.status === 401) {
                    const t: ToastSettings = {
                        message: "You aren't logged in",
                        background: 'variant-filled-error'
                    };
                    getToastStore().trigger(t);
                }
            }
        }
    }

    setSidebarContent({
        component: PostTags,
        data: {
            editMode: store,
            tags: data.post.tags,
            source: data.post.source,
            deletePostFunc: deletePost
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
            const url = $page.url;
            await goto('/');
            await goto(url);
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
