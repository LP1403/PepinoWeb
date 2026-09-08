import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
export default function GameModal({ title, children, onClose }: { title: string; children: ReactNode; onClose: () => void }) {
    const ref = useRef<HTMLDialogElement>(null);
    useEffect(() => { const d = ref.current!; d.showModal(); return () => d.close(); }, []);
    return <dialog ref={ref} className="game-modal" aria-labelledby="modal-title" onCancel={e => { e.preventDefault(); onClose(); }}>
        <h2 id="modal-title">{title}</h2>{children}
    </dialog>;
}
