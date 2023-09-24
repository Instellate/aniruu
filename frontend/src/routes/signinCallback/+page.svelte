<script lang="ts">
    import { goto } from '$app/navigation';
    import { page } from '$app/stores';
    import { client } from '$lib';
    import { ApiError, type ClaimUsername } from '$lib/client';
    import { hideSidebar, userStore } from '$lib/stores';

    let input = '';
    let errorMessage = '';

    const url = $page.url;
    const newAccount = url.searchParams.get('newAccount') ?? 'false';
    const token = url.searchParams.get('token');
    if (token === null) {
        goto('/');
    }

    async function onSubmitClick(): Promise<void> {
        if (input === null) {
            errorMessage = 'Name cannot be empty';
            return;
        }

        const body: ClaimUsername = {
            name: input,
            temporaryToken: token ?? ''
            // Token should NEVER hopefully be null as the goto statement at L18 should complete before it gets here.
        };

        try {
            const response = await client.account.accountClaimUsername(body);
            userStore.set({
                sessionToken: response.sessionToken
            });

            window.localStorage.setItem('token', response.sessionToken);
            await goto('/');
        } catch (err: unknown) {
            if (err instanceof ApiError) {
                switch (err.status) {
                    case 400:
                        errorMessage = 'Username contains invalid characters';
                        return;
                    case 404:
                        errorMessage = 'Please sign in again';
                        return;
                    case 409:
                        errorMessage = 'Username is already in use';
                        return;
                    case 429:
                        errorMessage = 'Please wait a bit';
                        return;
                }
                if (err.status !== 201) {
                    errorMessage = 'Something unepexteced happened';
                }
            } else {
                console.log('Unindentifiable error: ', err);
            }

            return;
        }
    }

    function getRandomName(): string {
        const names: readonly string[] = [
            'instellate',
            'flazepe',
            'karar.frontend.guy123',
            'rule34.hater',
            'safe.as.always',
            'cunnysseur'
        ] as const;

        const randomNum = Math.floor(Math.random() * names.length);

        return names[randomNum];
    }

    function onInputChange() {
        errorMessage = '';
    }

    hideSidebar();

    if (newAccount === 'false') {
        userStore.set({
            sessionToken: token ?? ''
        });
        window.localStorage.setItem('token', token ?? '');
        goto('/');
    }
</script>

<div class="container mx-auto flex justify-center items-center h-full">
    <div
        class="bg-surface-900 rounded-md flex justify-center items-center flex-col gap-8 p-16"
    >
        <strong class="self-center h3">Finalise your account</strong>
        <form action="" class="flex flex-col">
            <input
                bind:value={input}
                on:input={onInputChange}
                class="input variant-form-material w-96"
                class:input-error={errorMessage}
                title="Choose your username"
                type="text"
                placeholder={getRandomName()}
            />
            <strong class="text-red-500 text-xs">{errorMessage}&nbsp;</strong>
        </form>
        <button class="btn variant-filled w-auto" on:click={onSubmitClick}
            >Create Account</button
        >
    </div>
</div>
