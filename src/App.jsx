import { useState, useRef, useEffect } from 'react';
import { C } from './tokens';
import { INIT_INCIDENTS } from './data/mockIncidents';
import { DETAIL_DB, getDetail } from './data/mockDetails';
import Sidebar from './components/Sidebar';
import DashHome from './components/DashHome';
import Kanban from './components/Kanban';
import Detail from './components/Detail';
import Analytics from './components/Analytics';
import SettingsPage from './components/SettingsPage';
import Icon from './components/shared/Icon';

const TWEAK_DEFAULTS = {
  showSparklines: true,
  showNewBadge: true,
  compactCards: true,
  showConfidence: true,
};

export default function App() {
  const [page, setPage] = useState('home');
  const [incidents, setIncidents] = useState(INIT_INCIDENTS);
  const [selectedId, setSelectedId] = useState(null);
  const [statusFilter, setStatusFilter] = useState('all');
  const [tweaks, setTweaks] = useState(TWEAK_DEFAULTS);
  const [alertFlash, setAlertFlash] = useState(false);
  const prevHighRef = useRef(INIT_INCIDENTS.filter(i => i.score.eventization_score >= 0.85 && i.isNew).length);

  useEffect(() => {
    const highNew = incidents.filter(i => i.score.eventization_score >= 0.85 && i.isNew).length;
    if (highNew > prevHighRef.current) {
      setAlertFlash(true);
      setTimeout(() => setAlertFlash(false), 2000);
    }
    prevHighRef.current = highNew;
  }, [incidents]);

  const handleSelect = (id) => { setSelectedId(id); setPage('detail'); };
  const handleBack  = () => { setSelectedId(null); setPage('incidents'); };

  const handleStatusChange = (id, newStatus, note) => {
    const prevStatus = incidents.find(i => i.incident_id === id)?.status;

    setIncidents(prev => prev.map(i =>
      i.incident_id === id ? { ...i, status: newStatus, isNew: false } : i
    ));

    const det = DETAIL_DB[id] || getDetail(id);
    det.operator.note = note;
    if (!det.status_history_preview) det.status_history_preview = [];
    det.status_history_preview.push({
      from: prevStatus || null,
      to: newStatus,
      actor: { type: 'operator', id: 'operator_01' },
      at: new Date().toISOString(),
      note: note || null,
    });

    if (newStatus === 'confirmed' && !det.ai_output) {
      setTimeout(() => {
        det.ai_output = {
          summary: {
            lines: [
              `${id} 사건이 confirmed intrusion으로 판정되었습니다.`,
              '사람 객체가 경계선을 통과한 뒤 보호 구역에 진입했고 사건화 조건이 충족되었습니다.',
              '운영자는 현장 확인과 후속 대응 여부를 최종 판단해야 합니다.',
            ],
          },
          checklist: {
            items: ['CCTV 클립 재확인', '현장 인력에게 상황 전파', '인근 카메라와 출입기록 교차 확인'],
          },
          report_draft: {
            markdown: `## 1. 사건 개요\n- ID: ${id}\n- 발생: ${new Date().toLocaleString('ko-KR')}\n\n## 2. 상세 내용\n사람 객체가 경계선을 통과하여 confirmed intrusion 판정.\n\n## 3. 조치 사항\n- 현장 확인 필요`,
          },
          meta: {
            prompt_family: 'intrusion_assist',
            prompt_version: 'v2_structured',
            generated_at: new Date().toISOString(),
          },
        };
        const reportId = `AIR-2026-${id.split('-')[2]}`;
        det.report_summary = {
          report_available: true,
          report_id: reportId,
          report_generated_at: new Date().toISOString(),
        };
        setIncidents(prev => [...prev]);
      }, 1800);
    }
  };

  const newCount = incidents.filter(i => i.isNew).length;
  const selInc = incidents.find(i => i.incident_id === selectedId);

  return (
    <div style={{
      height: '100vh', display: 'flex', overflow: 'hidden', position: 'relative',
      background: `radial-gradient(ellipse 75% 90% at -5% 50%, rgba(210,85,20,0.22) 0%, rgba(170,65,15,0.12) 35%, rgba(130,50,10,0.04) 55%, transparent 70%), radial-gradient(ellipse 40% 60% at 50% 100%, rgba(180,70,10,0.06) 0%, transparent 60%), #0c0a09`,
    }}>
      {alertFlash && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(220,30,30,0.18)', pointerEvents: 'none', zIndex: 9998, animation: 'alertFlash 2s ease forwards' }} />
      )}

      <Sidebar
        page={page === 'detail' ? 'incidents' : page}
        setPage={p => { setPage(p); setSelectedId(null); }}
        newCount={newCount}
      />

      <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
        {page === 'home' && (
          <DashHome incidents={incidents} setPage={setPage} onSelect={handleSelect} tweaks={tweaks} />
        )}
        {page === 'incidents' && (
          <Kanban
            incidents={incidents}
            onSelect={handleSelect}
            statusFilter={statusFilter}
            setStatusFilter={setStatusFilter}
            onStatusChange={handleStatusChange}
            tweaks={tweaks}
          />
        )}
        {page === 'detail' && selInc && (
          <Detail
            inc={selInc}
            incidents={incidents}
            onBack={handleBack}
            onStatusChange={handleStatusChange}
            tweaks={tweaks}
          />
        )}
        {page === 'analytics' && (
          <Analytics incidents={incidents} onSelect={handleSelect} />
        )}
        {page === 'settings' && (
          <SettingsPage />
        )}
        {!['home', 'incidents', 'detail', 'analytics', 'settings'].includes(page) && (
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column', gap: 12 }}>
            <Icon name="monitor" size={24} color={C.fg4} />
            <span style={{ fontFamily: C.mono, fontSize: 10, color: C.fg4, letterSpacing: '1.4px', textTransform: 'uppercase' }}>{page} — 준비 중</span>
          </div>
        )}
      </div>
    </div>
  );
}
