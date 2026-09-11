import assert from 'node:assert/strict';
import {createFrameLimiter} from '../src/game3d/frameLimiter.ts';

for (const refresh of [50, 60, 75, 100, 120, 144, 165, 200, 240]) {
    for (const target of [30, 60]) {
        const limiter = createFrameLimiter();
        let frames = 0;
        for (let i = 0; i < refresh * 10; i++) {
            if (limiter.shouldRender(i * 1000 / refresh, target)) frames++;
        }
        assert.ok(Math.abs(frames / 10 - Math.min(refresh, target)) <= .1,
            `${refresh} Hz / target ${target}: got ${frames / 10} FPS`);
    }
}
const limiter = createFrameLimiter();
assert.equal(limiter.shouldRender(0, 60), true);
assert.equal(limiter.shouldRender(5000, 60), true);
assert.equal(limiter.shouldRender(5001, 60), false, 'No burst after a long stall');
limiter.reset();
assert.equal(limiter.shouldRender(5002, 30), true, 'Resume immediately after reset');
assert.equal(limiter.shouldRender(5007, 30), false);
console.log('PASS: 50–240 Hz, 30/60 FPS targets, long stalls and reset');
