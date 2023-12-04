<script lang="ts">
    import { env } from '$env/dynamic/public';
    import { hideSidebar, sidebarContent } from '$lib/stores';
    import type { PageData } from './$types';

    export let data: PageData;

    sidebarContent.set(null);

    const icon: Record<string, string> = {
        google: 'google_logo_black.svg',
        github: 'github_logo_black.svg',
        discord: 'discord_logo_black.svg'
    };

    hideSidebar();
</script>

<svelte:head>
    <title>Sign in | {env.PUBLIC_TITLE}</title>
</svelte:head>

<div class="container mx-auto flex justify-center items-center h-full">
    <div
        class="bg-surface-900 rounded-md flex justify-center flex-col gap-8 p-16 py-8 items-stretch"
    >
        <strong class="self-center h3 mb-4">Sign In</strong>
        {#each data.uris as uri}
            <a href={uri.uri} class="btn variant-filled flex-1">
                {#if icon[uri.service.toLowerCase()] !== undefined}
                    <img
                        src="/{icon[uri.service.toLowerCase()]}"
                        alt="{uri.service} icon"
                        class="h-8 w-auto"
                    />
                {/if}
                <span>Sign in with {uri.service}</span>
            </a>
        {/each}
    </div>
</div>
