/** Keep fractional frame time across display refreshes without catch-up bursts. */
export function createFrameLimiter() {
    let nextFrame: number | undefined;
    return {
        reset() { nextFrame = undefined; },
        shouldRender(now: number, fps: number): boolean {
            const interval = 1000 / fps;
            if (nextFrame === undefined) {
                nextFrame = now + interval;
                return true;
            }
            // Small tolerance for timestamp rounding at refresh rates such as 60 Hz.
            if (now + .1 < nextFrame) return false;
            const elapsedSlots = Math.max(1, Math.floor((now - nextFrame + .1) / interval) + 1);
            nextFrame += elapsedSlots * interval;
            return true;
        }
    };
}
