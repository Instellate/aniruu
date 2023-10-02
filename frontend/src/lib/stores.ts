import { browser } from '$app/environment';
import { onMount, type ComponentType } from 'svelte';
import { writable } from 'svelte/store';
import type { UserPermission } from './client';
import { client } from '$lib';

export interface SidebarType {
    data: object;
    component: ComponentType;
}

export interface User {
    sessionToken: string;
    permission: UserPermission;
    id: number;
}

export interface Preference {
    prefersOriginalSize: boolean;
}

export const sidebarContent = writable<SidebarType | null>();
export const hideSidebarStore = writable<boolean>(false);
export const preference = writable<Preference>({
    prefersOriginalSize: false
});
export const userStore = writable<User | null>(null);

if (browser) {
    (async () => {
        const token = window.localStorage.getItem('token');
        if (token !== null) {
            client.request.config.TOKEN = token;
            const user = await client.user.userGetUserMe();

            userStore.set({
                sessionToken: token,
                permission: user.permission ?? (0 as UserPermission),
                id: user.id
            });
        }
    })();
}

export function setSidebarContent(data: SidebarType) {
    onMount(() => {
        sidebarContent.set(data);
        return () => sidebarContent.set(null);
    });
}

export function hideSidebar() {
    onMount(() => {
        hideSidebarStore.set(true);
        return () => hideSidebarStore.set(false);
    });
}
