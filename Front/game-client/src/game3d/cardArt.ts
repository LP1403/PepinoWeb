import type { Card } from '../types/Card';
export const suitColors: Record<Card['suit'], string> = { '♠': '#2675c9', '♥': '#da5463', '♦': '#dca12e', '♣': '#8763c4' };
const art = new Map<string, HTMLCanvasElement>();
export function cardCanvas(card?: Card): HTMLCanvasElement {
    const key = card ? `${card.suit}${card.value}` : 'back';
    const cached = art.get(key); if (cached) return cached;
    const canvas = document.createElement('canvas'); canvas.width = 360; canvas.height = 520;
    const c = canvas.getContext('2d')!;
    const gold = card?.suit === '♦' && card.value === 3;
    c.fillStyle = '#fffef5'; c.beginPath(); c.roundRect(2, 2, 356, 516, 24); c.fill();
    c.fillStyle = card ? suitColors[card.suit] : '#18395e';
    c.beginPath(); c.roundRect(14, 14, 332, 492, 16); c.fill();
    c.save(); c.translate(180, 260); c.rotate(0.48);
    c.fillStyle = card ? 'rgba(255,255,255,.92)' : '#a5d552';
    c.beginPath(); c.ellipse(0, 0, 128, 205, 0, 0, Math.PI * 2); c.fill(); c.restore();
    c.textAlign = 'center'; c.textBaseline = 'middle';
    if (card) {
        const value = card.value === 1 ? '1' : String(card.value);
        c.font = '900 188px Arial, sans-serif'; c.lineWidth = 5;
        c.strokeStyle = '#fff'; c.fillStyle = suitColors[card.suit];
        c.strokeText(value, 180, 262); c.fillText(value, 180, 262);
        c.font = 'bold 48px Arial'; c.fillText(card.suit, 180, 378);
        c.fillStyle = '#fffef5';
        c.font = 'bold 58px Arial'; c.fillText(value, 51, 64);
        c.font = '32px Arial'; c.fillText(card.suit, 51, 116);
        c.save(); c.translate(309, 456); c.rotate(Math.PI);
        c.font = 'bold 58px Arial'; c.fillText(value, 0, 0);
        c.font = '32px Arial'; c.fillText(card.suit, 0, 52); c.restore();
        if (gold || card.value === 2) {
            c.fillStyle = gold ? '#fff0a5' : '#fff'; c.font = 'bold 21px Arial';
            c.fillText(gold ? 'PEPINO DE ORO' : 'COMODÍN', 180, 461);
        }
    } else {
        c.save(); c.translate(180, 260); c.rotate(-0.25);
        c.fillStyle = '#225c43'; c.beginPath(); c.roundRect(-38, -124, 76, 184, 38); c.fill();
        c.fillStyle = '#bde975'; for (let i = 0; i < 7; i++) { c.beginPath(); c.arc((i % 2 ? 15 : -15), -95 + i * 20, 4, 0, Math.PI * 2); c.fill(); }
        c.font = '900 49px Arial'; c.lineWidth = 8; c.strokeStyle = '#18395e'; c.fillStyle = '#fffef5';
        c.strokeText('PEPINO', 0, 112); c.fillText('PEPINO', 0, 112); c.restore();
        c.fillStyle = '#fffef5'; c.font = 'bold 26px Arial'; c.fillText('P', 43, 47); c.fillText('P', 318, 475);
    }
    art.set(key, canvas); return canvas;
}
const urls = new Map<string, string>();
export function cardImage(card: Card) {
    const key = `${card.suit}${card.value}`;
    if (!urls.has(key)) urls.set(key, cardCanvas(card).toDataURL());
    return urls.get(key)!;
}
