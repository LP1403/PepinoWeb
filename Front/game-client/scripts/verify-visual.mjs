import { chromium } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import assert from 'node:assert/strict';

const out = join(tmpdir(), 'pepino-visual-qa');
mkdirSync(out, { recursive:true });
const browser = await chromium.launch({ channel:'msedge', headless:true });
const results=[];
try {
    for (const width of [1440,390]) for (const players of [2,4,8]) {
        const page = await browser.newPage({ viewport:{width,height:900}, deviceScaleFactor:width<600?3:1, hasTouch:width<600 });
        const errors=[];
        page.on('pageerror', e=>errors.push(e.message));
        await page.goto(`http://127.0.0.1:5174/?demo=1&players=${players}&hand=48`);
        await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
        const canvas=page.locator('canvas').first();
        const metrics=await canvas.evaluate(e=>({...e.dataset}));
        const hand=page.locator('.hand-scroll');
        await page.getByRole('button',{name:'Cartas siguientes'}).click();
        await page.waitForFunction(()=>document.querySelector('.hand-scroll').scrollLeft>0);
        await hand.evaluate(e=>{e.scrollLeft=0});
        // Lift a visible wildcard vertically, then drop into the play zone.
        const card=page.locator('.hand-card[data-value="2"]').last();
        const box=await card.boundingBox();
        await page.mouse.move(box.x+box.width*.7,box.y+box.height*.5);
        await page.mouse.down();
        await page.mouse.move(box.x+box.width*.7,box.y+box.height*.5-35,{steps:5});
        await page.locator('.drop-target').waitFor();
        const zone=await page.locator('.drop-target').boundingBox();
        await page.mouse.move(zone.x+zone.width/2,zone.y+zone.height/2,{steps:10});
        await page.mouse.up();
        await page.waitForFunction(()=>Number(document.querySelector('[data-revision]')?.dataset.revision)>1);
        if (width < 600) {
            const cdp=await page.context().newCDPSession(page);
            const r=await hand.boundingBox();
            const x=r.x+r.width*.8,y=r.y+r.height*.55;
            await cdp.send('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y}]});
            for(let i=1;i<=10;i++) await cdp.send('Input.dispatchTouchEvent',{type:'touchMove',touchPoints:[{x:x-i*18,y}]});
            await cdp.send('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
            await page.waitForFunction(()=>document.querySelector('.hand-scroll').scrollLeft>0);
            await cdp.detach();
        }
        await page.screenshot({path:join(out,`${width}-${players}.png`)});
        assert.deepEqual(errors,[]);
        results.push({width,players,metrics,errors,drag:'mouse passed',scroll:width<600?'navigation and emulated touch passed':'navigation passed'});
        await page.close();
    }
} finally { await browser.close(); }
writeFileSync(join(out,'results.json'),JSON.stringify(results,null,2));
console.log(JSON.stringify({out,results},null,2));
