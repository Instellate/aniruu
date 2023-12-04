<script lang="ts">
    import { client } from '$lib';
    import PostTags from './PostTags.svelte';
    import { preference, setSidebarContent, userStore } from '$lib/stores';
    import { writable } from 'svelte/store';
    import type { PageData } from './$types';
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';
    import { ApiError, type TagType } from '$lib/client';
    import { Paginator, getToastStore } from '@skeletonlabs/skeleton';
    import Comment from './Comment.svelte';
    import { env } from '$env/dynamic/public';

    const toastStore = getToastStore();

    export let data: PageData;

    const store = writable<boolean>(false);

    async function deletePost() {
        try {
            await client.post.postDeletePost(data.post.id);
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                if (err.status === 403) {
                    toastStore.trigger({
                        message: "You aren't logged in",
                        background: 'variant-filled-error'
                    });
                } else if (err.status === 401) {
                    toastStore.trigger({
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
            deletePostFunc: deletePost,
            author: data.post.createdBy.id
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
    let sourceStr = data.post.source;

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

    let commentError = '';
    let value = '';
    async function createComment() {
        if (value) {
            try {
                const id = await client.post.postCreateComment(data.post.id, {
                    content: value
                });

                if (Math.ceil((data.commentPage.total + 1) / 5) <= data.currentPage) {
                    data.commentPage.comments = [
                        ...data.commentPage.comments,
                        {
                            content: value,
                            author: {
                                id: $userStore?.id ?? 0,
                                name: $userStore?.name ?? ''
                            },
                            createdAt: Date.now(),
                            id: id
                        }
                    ];
                }

                value = '';
            } catch (err: unknown) {
                commentError = 'Something unknown happened';
            }
        } else {
            commentError = 'Comment cannot be empty';
        }
    }

    async function reloadComments(pageNum: number) {
        data.commentPage = await client.post.postGetComments(data.post.id, pageNum);
    }

    async function nextPage(e: CustomEvent<number>) {
        const pageNum = e.detail + 1;

        try {
            reloadComments(pageNum);
        } catch (e: unknown) {
            // Empty
        }
        const url = $page.url;
        url.searchParams.set('commentPage', String(pageNum));
        window.history.pushState({}, '', url);
        data.currentPage = pageNum;
    }
</script>

<svelte:head>
    <title>Post {data.post.id}</title>
    <meta property="og:title" content="{env.PUBLIC_TITLE} - Post {data.post.id}" />
    <meta property="og:type" content="website" />
    <meta
        property="og:image"
        content="{client.request.config.BASE}{data.post.location}"
    />
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
            <button class="btn variant-ghost-surface w-fit" on:click={updatePost}>
                Submit
            </button>
        </div>
    {/if}

    <div class="flex flex-col gap-2 mt-2">
        <strong class="text-lg">Comments:</strong>
        <div class="max-w-xs lg:max-w-none w-[35rem] flex flex-col gap-4">
            {#each data.commentPage.comments as comment}
                <Comment
                    postId={data.post.id}
                    {comment}
                    on:delete={async () => reloadComments(data.currentPage)}
                />
            {/each}
        </div>

        <Paginator
            showNumerals
            showFirstLastButtons
            on:page={nextPage}
            controlVariant="variant-ghost-surface"
            settings={{
                page: data.currentPage - 1,
                limit: 5,
                size: data.commentPage.total,
                amounts: []
            }}
        />

        <textarea
            name="Create comment"
            cols="40"
            rows="5"
            class="w-fit textarea mt-2 max-w-xs lg:max-w-none"
            class:input-error={commentError}
            placeholder="Comment content..."
            bind:value
            on:input={() => (commentError = '')}
        />
        <strong class="text-red-500 text-xs" class:hidden={!commentError}>
            {commentError}&nbsp;
        </strong>
        <button class="btn variant-filled w-fit" on:click={createComment}>
            Submit comment
        </button>
    </div>
</div>
