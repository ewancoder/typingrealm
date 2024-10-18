export function createLock(): Lock {
    let nextLock: Promise<void> | undefined = undefined;
    const resolves: (() => void)[] = [];

    return {
        wait: async function () {
            const lock = new Promise<void>(resolve => resolves.push(resolve));

            // TODO: These operations should be atomic.
            const thisLock = nextLock;
            nextLock = lock;
            await thisLock;
        },

        release: function () {
            const resolve = resolves.shift();
            if (resolve !== undefined) {
                resolve();
            }
        }
    };
}

export interface Lock {
    wait: () => Promise<void>;
    release: () => void;
}
