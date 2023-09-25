<script lang="ts">
    import { client } from '$lib';
    import { ApiError, type PostComment } from '$lib/client';
    import { getToastStore, popup } from '@skeletonlabs/skeleton';

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
            <!-- This svg was porivded by https://heroicons.com/ -->
            <svg
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
                stroke-width="1.5"
                stroke="currentColor"
                class="w-6 h-6"
            >
                <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    d="M12 6.75a.75.75 0 110-1.5.75.75 0 010 1.5zM12 12.75a.75.75 0 110-1.5.75.75 0 010 1.5zM12 18.75a.75.75 0 110-1.5.75.75 0 010 1.5z"
                />
            </svg>
        </button>
        <div data-popup="deleteCommentPopup-{comment.id}" class="card opacity-0" inert>
            <!-- TODO: Make these buttons only appear for appropriate users -->
            <div class="flex flex-col gap-0.5">
                <button
                    on:click={() => editMode = true}
                    class="hover:bg-slate-600 duration-150 p-2 rounded-md"
                >
                    Edit
                </button>
                <button
                    on:click={deleteComment}
                    class="hover:bg-slate-600 duration-150 p-2 rounded-md"
                >
                    Delete
                </button>
            </div>
        </div>
    </div>
    {#if editMode}
        <textarea class="textarea" bind:value={newContent} />
        <div class="flex flex-row gap-2">
            <button class="btn btn-sm variant-filled" on:click={editComment}>Submit</button>
            <button class="btn btn-sm variant-filled" on:click={() => editMode = false}>
                Cancel
            </button>
        </div>
    {:else}
        <p>{comment.content}</p>
    {/if}
    <small>Created at {formatDateTime(comment.createdAt)}</small>
</div>
