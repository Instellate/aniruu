<script lang="ts">
    import '../app.postcss';
    import {
        AppShell,
        AppBar,
        storePopup,
        type PopupSettings,
        popup,
        initializeStores,
        Toast
    } from '@skeletonlabs/skeleton';
    import {
        computePosition,
        autoUpdate,
        offset,
        shift,
        flip,
        arrow
    } from '@floating-ui/dom';
    import SearchBar from '$lib/SearchBar.svelte';
    import { hideSidebarStore, sidebarContent, userStore } from '$lib/stores';
    import { goto } from '$app/navigation';
    import { browser } from '$app/environment';
    import { client } from '$lib';

    initializeStores();

    async function signOut(): Promise<void> {
        if (browser) {
            try {
                if ($userStore !== null) {
                    await client.account.accountDeleteSession($userStore.sessionToken);
                } else {
                    console.log('Cannot log out');
                }
            } catch (err: unknown) {
                console.log("Couldn't delete sesssion");
                return;
            }

            window.localStorage.removeItem('token');
            console.log('Removed the item?', window.localStorage.getItem('token'));
            userStore.set(null);
            await goto('/');
        } else {
            console.log('Cannot do this action on server');
        }
    }

    storePopup.set({ computePosition, autoUpdate, offset, shift, flip, arrow });

    const accountPopupSetting: PopupSettings = {
        event: 'click',
        target: 'accountPopup',
        placement: 'bottom-end'
    };
</script>

<svelte:head>
    <title>Upload a post</title>
</svelte:head>

<Toast />

<AppShell slotSidebarLeft="bg-surface-900 w-80 {$hideSidebarStore ? 'hidden' : ''}">
    <svelte:fragment slot="header">
        <AppBar>
            <svelte:fragment slot="lead">
                <strong class="text-xl uppercase"><a href="/">Aniruu</a></strong>
            </svelte:fragment>
            <svelte:fragment slot="trail">
                <a href="/upload" class="btn variant-filled">Upload</a>

                <button class="btn variant-ghost-surface" use:popup={accountPopupSetting}>
                    Account
                </button>

                <div class="card p-4 variant filled-primary" data-popup="accountPopup">
                    {#if $userStore !== null}
                        <div class="flex flex-col gap-2">
                            <a
                                class="btn bg-gradient-to-br variant-gradient-secondary-tertiary"
                                href="/profile"
                            >
                                Profile
                            </a>
                            <a
                                class="btn bg-gradient-to-br variant-gradient-secondary-tertiary"
                                href="/settings"
                            >
                                Settings
                            </a>
                            <button
                                on:click={signOut}
                                class="btn bg-gradient-to-br variant-gradient-secondary-tertiary"
                                >Sign out
                            </button>
                        </div>
                    {:else}
                        <a
                            href="/signin"
                            class="btn bg-gradient-to-br variant-gradient-secondary-tertiary"
                            >Sign In</a
                        >
                    {/if}
                </div></svelte:fragment
            >
        </AppBar>
    </svelte:fragment>

    <svelte:fragment slot="sidebarLeft">
        <div class="p-5 flex flex-col gap-3">
            <SearchBar />
            {#if $sidebarContent}
                <svelte:component
                    this={$sidebarContent.component}
                    {...$sidebarContent.data}
                />
            {/if}
        </div>
    </svelte:fragment>

    <slot />
</AppShell>
