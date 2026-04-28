import { useState } from 'react';
import { C } from '../tokens';
import Icon from './shared/Icon';

export default function FAModal({ onConfirm, onCancel }) {
  const [note, setNote] = useState('');
  const valid = note.trim().length > 0;
  return (
    <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.78)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9999 }}>
      <div style={{ background: C.bg2, border: '1px solid rgba(239,68,68,0.3)', borderRadius: C.R, padding: 28, width: 460, display: 'flex', flexDirection: 'column', gap: 16, animation: 'modalIn 200ms ease' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <Icon name="x-circle" size={16} color="rgba(239,68,68,0.9)" />
          <span style={{ fontFamily: C.mono, fontSize: 12, color: C.fg1, letterSpacing: '1.4px', textTransform: 'uppercase' }}>FALSE ALARM 처리</span>
        </div>
        <div style={{ fontFamily: C.sans, fontSize: 13, color: C.fg3, lineHeight: 1.6 }}>이 사건을 허위경보로 처리합니다. 판단 근거를 반드시 입력해야 합니다.</div>
        <textarea
          value={note}
          onChange={e => setNote(e.target.value)}
          rows={4}
          autoFocus
          placeholder="판단 근거를 입력하세요..."
          style={{ width: '100%', fontFamily: C.mono, fontSize: 11, background: C.bg3, color: C.fg1, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '10px 12px', resize: 'none', lineHeight: 1.6, outline: 'none', boxSizing: 'border-box' }}
        />
        <div style={{ display: 'flex', gap: 8, justifyContent: 'flex-end' }}>
          <button onClick={onCancel} style={{ background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '9px 18px', fontFamily: C.mono, fontSize: 9, color: C.fg3, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer' }}>CANCEL</button>
          <button
            onClick={() => valid && onConfirm(note)}
            style={{ background: valid ? 'rgba(239,68,68,0.1)' : 'transparent', border: `1px solid ${valid ? 'rgba(239,68,68,0.4)' : 'rgba(239,68,68,0.15)'}`, borderRadius: C.R, padding: '9px 18px', fontFamily: C.mono, fontSize: 9, color: valid ? 'rgba(239,68,68,0.9)' : 'rgba(239,68,68,0.3)', letterSpacing: '1px', textTransform: 'uppercase', cursor: valid ? 'pointer' : 'not-allowed', transition: 'all 150ms ease' }}
          >
            CONFIRM FALSE ALARM
          </button>
        </div>
      </div>
    </div>
  );
}
