import {chromium} from '@playwright/test';
import assert from 'node:assert/strict';
const browser=await chromium.launch({channel:'msedge',headless:true});
try {
    for(const width of [1440,390]){
        const page=await browser.newPage({viewport:{width,height:900}});
        const errors=[];page.on('pageerror',e=>errors.push(e.message));
        await page.goto('http://127.0.0.1:5174/?demo=1&players=4&hand=48');
        await page.waitForFunction(()=>document.querySelector('canvas')?.dataset.fps);
        const hand=page.locator('.hand-scroll');
        const wild=page.locator('.hand-card[data-value="2"]');
        const drag=async(card,stale=false)=>{
            await card.scrollIntoViewIfNeeded();
            const b=await card.boundingBox();
            await page.mouse.move(b.x+b.width*.7,b.y+b.height*.5);await page.mouse.down();
            await page.mouse.move(b.x+b.width*.7,b.y+b.height*.5-40,{steps:5});
            await page.locator('.drop-target').waitFor();
            if(stale) await page.getByRole('button',{name:'PASAR',exact:true}).evaluate(e=>e.click());
            const z=await page.locator('.drop-target').boundingBox();
            await page.mouse.move(z.x+z.width/2,z.y+z.height/2,{steps:10});await page.mouse.up();
        };
        // Keyboard selection of a group remains available with WebGL card faces.
        await wild.nth(2).focus();await page.keyboard.press('Space');
        await wild.nth(3).focus();await page.keyboard.press('Space');
        assert.equal(await page.locator('.hand-card[aria-pressed="true"]').count(),2);
        await drag(wild.last());
        await page.getByRole('dialog').waitFor();
        assert.equal(await page.locator('.confirm-cards img').count(),2);
        await page.keyboard.press('Escape');
        assert.equal(await page.getByRole('dialog').count(),0);
        await page.getByRole('button',{name:/Limpiar/}).click();
        // Lower-value card must not open confirmation over a four.
        await drag(page.locator('.hand-card[data-value="3"]').last());
        assert.equal(await page.getByRole('dialog').count(),0);
        await page.getByRole('button',{name:/Limpiar/}).click();
        // A new revision while dragging invalidates the drop.
        await drag(wild.last(),true);
        assert.equal(await page.getByRole('dialog').count(),0);
        // Real mouse panning (not programmatic scroll) must move the fan.
        await hand.evaluate(e=>e.scrollLeft=0);
        const h=await hand.boundingBox();
        const x=h.x+h.width*.8,y=h.y+h.height*.5;
        await page.mouse.move(x,y);await page.mouse.down();
        await page.mouse.move(x-160,y,{steps:10});await page.mouse.up();
        assert.ok(await hand.evaluate(e=>e.scrollLeft)>50);
        // Confirming a group updates the rendered hand and starts the flight.
        await wild.nth(2).focus();await page.keyboard.press('Space');
        await wild.nth(3).focus();await page.keyboard.press('Space');
        await page.getByRole('button',{name:'JUGAR',exact:true}).click();
        await page.getByRole('button',{name:'CONFIRMAR',exact:true}).click();
        await page.waitForFunction(()=>document.querySelectorAll('.hand-card').length===46);
        await page.setViewportSize({width:width===390?844:1000,height:width===390?390:700});
        await page.waitForTimeout(300);
        assert.equal(await page.locator('.local-cards-3d').count(),1);
        assert.deepEqual(errors,[]);
        console.log(`PASS ${width}: keyboard/group/invalid/stale/mouse pan/confirmed play/resize`);
        await page.close();
    }
}finally{await browser.close();}
