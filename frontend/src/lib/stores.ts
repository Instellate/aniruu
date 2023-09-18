import { browser } from '$app/environment';
import { onMount, type ComponentType } from 'svelte';
import { writable } from 'svelte/store';

export interface SidebarType {
    data: object;
    component: ComponentType;
}

export interface User {
    sessionToken: string;
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
    const token = window.localStorage.getItem('token');
    if (token !== null) {
        userStore.set({
            sessionToken: token,
        });
    }
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
