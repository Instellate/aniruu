<script lang="ts">
    import '../app.postcss';
    import {
        AppShell,
        AppBar,
        storePopup,
        type PopupSettings,
        popup,
        initializeStores,
        Toast,
        Drawer,
        getDrawerStore
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
    import { client, hasFlag } from '$lib';
    import { UserPermission } from '$lib/client';
    import { env } from '$env/dynamic/public';

    initializeStores();
    storePopup.set({ computePosition, autoUpdate, offset, shift, flip, arrow });

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

    const drawerStore = getDrawerStore();
    function openDrawer() {
        drawerStore.open({});
    }

    const accountPopupSetting: PopupSettings = {
        event: 'click',
        target: 'accountPopup',
        placement: 'bottom-end'
    };
</script>

<Toast />
<Drawer>
    <div class="p-5 flex flex-col gap-3">
        <SearchBar />
        {#if $sidebarContent}
            <svelte:component
                this={$sidebarContent.component}
                {...$sidebarContent.data}
            />
        {/if}
    </div>
</Drawer>

<AppShell
    slotSidebarLeft="bg-surface-900 w-0 lg:w-80 {$hideSidebarStore ? 'hidden' : ''}"
>
    <svelte:fragment slot="header">
        <AppBar>
            <svelte:fragment slot="lead">
                <div class="flex items-center">
                    <button class="lg:hidden btn btn-sm mr-4" on:click={openDrawer}>
                        {#if !$hideSidebarStore}
                        <span>
                            <svg viewBox="0 0 100 80" class="fill-token w-4 h-4">
                                <rect width="100" height="20" />
                                <rect y="30" width="100" height="20" />
                                <rect y="60" width="100" height="20" />
                            </svg>
                        </span>
                        {/if}
                    </button>
                </div>
                <strong class="text-xl uppercase">
                    <a href="/">{env.PUBLIC_TITLE}</a>
                </strong>
            </svelte:fragment>
            <svelte:fragment slot="trail">
                {#if hasFlag($userStore?.permission ?? 0, UserPermission._1)}
                    <a href="/upload" class="btn variant-filled">Upload</a>
                {/if}

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
                            >
                                Sign out
                            </button>
                        </div>
                    {:else}
                        <a
                            href="/signin"
                            class="btn bg-gradient-to-br variant-gradient-secondary-tertiary"
                        >
                            Sign In
                        </a>
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
