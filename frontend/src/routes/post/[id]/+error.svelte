<script lang="ts">
    import { page } from '$app/stores';
    import { hideSidebar } from '$lib/stores';

    hideSidebar();
</script>

{#if $page.error?.message === 'notFound'}
    <div class="flex justify-center items-center mx-auto h-full">
        <strong class="text-xl">Post not found</strong>
    </div>
{:else}
    <div class="flex justify-center items-center mx-auto h-full">
        <strong class="text-xl">Uh oh, something unexpected happened</strong>
    </div>
{/if}
