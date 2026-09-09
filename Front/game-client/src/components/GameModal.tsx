import { useEffect, useRef, useId } from 'react';
import type { ReactNode } from 'react';
export default function GameModal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
    const ref = useRef<HTMLDialogElement>(null);
    const titleId=useId();
    useEffect(() => { const d = ref.current!; d.showModal(); return () => d.close(); }, []);
    return <dialog ref={ref} className="game-modal" aria-labelledby={titleId} onCancel={e => { e.preventDefault(); onClose(); }}>
        <h2 id={titleId}>{title}</h2>{children}
    </dialog>;
}
