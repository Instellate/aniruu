<script lang="ts">
    import { client, hasFlag } from '$lib';
    import { ApiError, UserPermission, type PostComment } from '$lib/client';
    import { getToastStore, popup } from '@skeletonlabs/skeleton';
    import { userStore } from '$lib/stores';
    import SettingsSvg from './SettingsSvg.svelte';
    import { createEventDispatcher } from 'svelte';

    const dispatch = createEventDispatcher();

    export let comment: PostComment;
    export let postId: number;
    const toastStore = getToastStore();

    async function deleteComment() {
        try {
            await client.post.postDeleteComment(postId, comment.id);
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                if (err.status === 403) {
                    toastStore.trigger({
                        message: 'You cannot delete this comment',
                        background: 'variant-filled-error'
                    });
                }
            }
        }
        dispatch('delete');
    }

    let editMode = false;
    let newContent = comment.content;

    async function editComment() {
        try {
            await client.post.postEditComment(postId, comment.id, {
                content: newContent
            });
            comment.content = newContent;
            editMode = false;
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                if (err.status === 403) {
                    toastStore.trigger({
                        message: 'You cannot edit this comment',
                        background: 'variant-filled-error'
                    });
                }
            }
        }
    }

    function formatDateTime(dateTime: number): string {
        const en = Intl.DateTimeFormat('en-gb', {
            month: '2-digit',
            day: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
        return en.format(dateTime);
    }
</script>

<div class="bg-surface-800 rounded-md space-y-2 flex flex-col gpa-0.5 p-2">
    <div class="flex flex-row justify-between">
        <strong class="text-lg">{comment.author.name}</strong>
        <button
            use:popup={{
                event: 'click',
                placement: 'bottom',
                target: `deleteCommentPopup-${comment.id}`
            }}
        >
            <SettingsSvg />
        </button>
        <div
            data-popup="deleteCommentPopup-{comment.id}"
            class="card opacity-0 overflow-hidden"
            inert
        >
            {#if $userStore?.id === comment.author.id}
                <div class="flex flex-col gap-0.5">
                    <button
                        on:click={() => (editMode = true)}
                        class="hover:bg-slate-600 duration-150 p-2"
                    >
                        Edit
                    </button>
                    <button
                        on:click={deleteComment}
                        class="hover:bg-slate-600 duration-150 p-2"
                    >
                        Delete
                    </button>
                </div>
            {:else if hasFlag($userStore?.permission ?? 0, UserPermission._128)}
                <div class="flex flex-col gap-0.5">
                    <button
                        on:click={deleteComment}
                        class="hover:bg-slate-600 duration-150 p-2"
                    >
                        Delete
                    </button>
                </div>
            {/if}
        </div>
    </div>
    {#if editMode}
        <textarea class="textarea" bind:value={newContent} />
        <div class="flex flex-row gap-2">
            <button class="btn btn-sm variant-filled" on:click={editComment}>
                Submit
            </button>
            <button class="btn btn-sm variant-filled" on:click={() => (editMode = false)}>
                Cancel
            </button>
        </div>
    {:else}
        <p>{comment.content}</p>
    {/if}
    <small>Created at {formatDateTime(comment.createdAt)}</small>
</div>
