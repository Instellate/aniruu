<script lang="ts">
    import { client } from '$lib';
    import PostTags from '$lib/PostTags.svelte';
    import { preference, setSidebarContent } from '$lib/stores';
    import { writable } from 'svelte/store';
    import type { PageData } from './$types';
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';
    import { ApiError, type TagType } from '$lib/client';
    import { getToastStore } from '@skeletonlabs/skeleton';
    import Comment from './Comment.svelte';

    export let data: PageData;

    const store = writable<boolean>(false);

    async function deletePost() {
        try {
            await client.post.postDeletePost(data.post.id);
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                if (err.status === 403) {
                    getToastStore().trigger({
                        message: "You aren't logged in",
                        background: 'variant-filled-error'
                    });
                } else if (err.status === 401) {
                    getToastStore().trigger({
                        message: "You aren't logged in",
                        background: 'variant-filled-error'
                    });
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
            location: `${client.request.config.BASE}${data.post.location}`,
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

    let editValue = tagsToString();
    let sourceStr = '';

    async function updatePost() {
        try {
            await client.post.postEditPost(data.post.id, {
                tags: editValue,
                source: sourceStr === '' ? undefined : sourceStr
            });
            const url = $page.url;
            await goto('/');
            await goto(url);
        } catch (err: unknown) {
            console.error(err);
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

    let value = '';
    async function createComment() {
        if (value) {
            try {
                await client.post.postCreateComment(data.post.id, {
                    content: value
                });
            } catch (err: unknown) {
                // Empty
            }
        } else {
            getToastStore().trigger({
                message: 'Comment cannot be empty',
                background: 'variant-filled-error'
            });
        }
    }
</script>

<svelte:head>
    <title>Post {data.post.id}</title>
</svelte:head>

<div class="px-5 py-5 flex flex-col">
    <button on:click={changeImgSize} class="w-fit">
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
            <input
                type="text"
                class="input variant-form-material w-fit"
                placeholder="Source"
                bind:value={sourceStr}
            />
            <button class="btn variant-ghost-surface w-fit" on:click={updatePost}
                >Submit</button
            >
        </div>
    {/if}

    <div class="flex flex-col gap-2 mt-2">
        <strong class="text-lg">Comments:</strong>
        <div class="w-[35rem] flex flex-col gap-4">
            {#each data.comments as comment}
                <Comment postId={data.post.id} {comment} />
            {/each}
        </div>
        <textarea
            name="Create comment"
            cols="40"
            rows="5"
            class="w-fit textarea mt-2"
            placeholder="Comment content..."
            bind:value
        />
        <button class="btn variant-filled w-fit" on:click={createComment}>
            Submit comment
        </button>
    </div>
</div>
