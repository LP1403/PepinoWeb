export interface SeatPosition { x: number; y: number; rotation: number; avatarX: number; avatarY: number }
export function seatPositions(count: number, portrait = false): SeatPosition[] {
    if (portrait && count > 3) return Array.from({ length: count }, (_, i) => ({
        x: (i % 4 + .5) / 4, y: .20 + Math.floor(i / 4) * .18,
        rotation: (i % 4 - 1.5) * .08,
        avatarX: (i % 4 + .5) / 4, avatarY: .115 + Math.floor(i / 4) * .18
    }));
    if (count === 1) return [{ x: .5, y: .29, rotation: 0, avatarX: .5, avatarY: .15 }];
    if (count === 2) return [
        { x: .24, y: .29, rotation: -.55, avatarX: .1, avatarY: .24 },
        { x: .76, y: .29, rotation: .55, avatarX: .9, avatarY: .24 }
    ];
    if (count === 3) return [
        { x: .22, y: .38, rotation: -.3, avatarX: .14, avatarY: .25 },
        { x: .5, y: .27, rotation: 0, avatarX: .5, avatarY: .14 },
        { x: .78, y: .38, rotation: .3, avatarX: .86, avatarY: .25 }
    ];
    return Array.from({ length: count }, (_, i) => {
        const a = Math.PI * (.07 + .86 * i / Math.max(1, count - 1));
        const x = .5 - Math.cos(a) * .36, y = .56 - Math.sin(a) * .4;
        return { x, y, rotation: (x - .5) * 1.4, avatarX: x, avatarY: y - .105 };
    });
}
