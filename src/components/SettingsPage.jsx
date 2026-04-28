import { useState } from 'react';
import { C } from '../tokens';
import Icon from './shared/Icon';

const SECTIONS = [
  { id: 'detection',     icon: 'activity',  label: '감지 설정' },
  { id: 'zones',         icon: 'map-pin',   label: '구역 규칙' },
  { id: 'notifications', icon: 'bell',      label: '알림' },
  { id: 'cameras',       icon: 'camera',    label: '카메라' },
  { id: 'realtime',      icon: 'radio',     label: 'Realtime' },
  { id: 'system',        icon: 'server',    label: '시스템' },
  { id: 'debug',         icon: 'terminal',  label: '디버그' },
];

export default function SettingsPage() {
  const [active, setActive] = useState('detection');
  const [vals, setVals] = useState({
    sensitivity: 72, minDuration: 2.0, minConfidence: 0.65, minScore: 0.60,
    lineEnabled: true, roiEnabled: true, durationEnabled: true,
    notifyNew: true, notifyConfirmed: true, notifyFalseAlarm: false, notifyEmail: false,
    emailAddr: 'ops@factory-a.co.kr',
    camLive: true, camRetention: 30, camFps: 15,
    realtimeEnabled: true, realtimePushNew: true, realtimePushStatus: true,
    debugMode: false, logLevel: 'warn', mockDelay: 800, showApiCalls: false,
    operatorId: 'operator_01', site: 'factory_A', timezone: 'Asia/Seoul',
  });
  const set = (k, v) => setVals(p => ({ ...p, [k]: v }));

  const Row = ({ label, sub, children }) => (
    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '13px 0', borderBottom: `1px solid ${C.border}` }}>
      <div>
        <div style={{ fontFamily: C.sans, fontSize: 13, color: C.fg1 }}>{label}</div>
        {sub && <div style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4, marginTop: 2 }}>{sub}</div>}
      </div>
      {children}
    </div>
  );

  const Toggle = ({ k }) => (
    <button onClick={() => set(k, !vals[k])} style={{ width: 36, height: 20, background: vals[k] ? C.orange : C.border2, borderRadius: 10, position: 'relative', transition: 'background 150ms', flexShrink: 0, cursor: 'pointer' }}>
      <div style={{ width: 14, height: 14, background: '#fff', borderRadius: '50%', position: 'absolute', top: 3, left: vals[k] ? 19 : 3, transition: 'left 150ms' }} />
    </button>
  );

  const Slider = ({ k, min, max, step = 1, unit = '' }) => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
      <input type="range" min={min} max={max} step={step} value={vals[k]} onChange={e => set(k, parseFloat(e.target.value))}
        style={{ width: 110, accentColor: C.orange, cursor: 'pointer' }} />
      <span style={{ fontFamily: C.mono, fontSize: 10, color: C.fg2, minWidth: 36, textAlign: 'right' }}>{vals[k]}{unit}</span>
    </div>
  );

  const Select = ({ k, options }) => (
    <select value={vals[k]} onChange={e => set(k, e.target.value)}
      style={{ background: C.bg3, color: C.fg2, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '5px 10px', fontFamily: C.mono, fontSize: 10, cursor: 'pointer', outline: 'none' }}>
      {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
    </select>
  );

  const renderContent = () => {
    if (active === 'detection') return (<>
      <Row label="감지 민감도"       sub="높을수록 더 많은 이벤트 감지 (false alarm 증가)"><Slider k="sensitivity"    min={30}  max={99}   unit="%" /></Row>
      <Row label="최소 지속시간"     sub="이 시간 이상 감지되어야 사건화됨"><Slider k="minDuration"   min={0.5} max={10}   step={0.1} unit="s" /></Row>
      <Row label="최소 confidence"   sub="detector_confidence_avg 임계값"><Slider k="minConfidence" min={0.3} max={0.99} step={0.01} /></Row>
      <Row label="최소 이벤트화 점수" sub="eventization_score 임계값"><Slider k="minScore"       min={0.3} max={0.99} step={0.01} /></Row>
      <Row label="Line Crossed 조건" sub="경계선 통과 여부 필수 확인"><Toggle k="lineEnabled" /></Row>
      <Row label="ROI Entered 조건"  sub="관심 구역 진입 여부 필수 확인"><Toggle k="roiEnabled" /></Row>
      <Row label="Duration 조건"     sub="체류 시간 조건 사용"><Toggle k="durationEnabled" /></Row>
    </>);

    if (active === 'zones') return (<>
      {['outer_fence_north','server_room_b2','warehouse_south','inner_gate_east','parking_west','roof_access_c','loading_dock_d'].map((z, i) => (
        <div key={z} style={{ padding: '12px 0', borderBottom: `1px solid ${C.border}` }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div>
              <div style={{ fontFamily: C.mono, fontSize: 11, color: C.fg1 }}>{z.replace(/_/g, ' ')}</div>
              <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, marginTop: 2 }}>
                정책: {['outer_fence_rule_v1','server_room_rule_v2','warehouse_rule_v2','gate_rule_v1','parking_rule_v1','roof_rule_v1','dock_rule_v1'][i]}
              </div>
            </div>
            <button style={{ width: 36, height: 20, background: i < 5 ? C.orange : C.border2, borderRadius: 10, position: 'relative', cursor: 'pointer' }}>
              <div style={{ width: 14, height: 14, background: '#fff', borderRadius: '50%', position: 'absolute', top: 3, left: i < 5 ? 19 : 3 }} />
            </button>
          </div>
        </div>
      ))}
    </>);

    if (active === 'notifications') return (<>
      <Row label="새 Incident 알림"   sub="incident_created 실시간 push"><Toggle k="notifyNew" /></Row>
      <Row label="Confirmed 알림"     sub="confirmed 전이 시 알림"><Toggle k="notifyConfirmed" /></Row>
      <Row label="False Alarm 알림"   sub="false_alarm 처리 시 알림"><Toggle k="notifyFalseAlarm" /></Row>
      <Row label="이메일 알림"         sub="Critical 이상 이벤트 이메일 전송"><Toggle k="notifyEmail" /></Row>
      <Row label="이메일 주소" sub="수신 주소">
        <input value={vals.emailAddr} onChange={e => set('emailAddr', e.target.value)}
          style={{ background: C.bg3, color: C.fg2, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '5px 10px', fontFamily: C.mono, fontSize: 10, outline: 'none', width: 200 }} />
      </Row>
    </>);

    if (active === 'cameras') return (<>
      <Row label="실시간 스트림"   sub="카메라 라이브 피드 활성화"><Toggle k="camLive" /></Row>
      <Row label="영상 보존 기간"  sub="클립 저장 일수"><Slider k="camRetention" min={7} max={90} unit="일" /></Row>
      <Row label="녹화 FPS"        sub="저장 프레임레이트"><Slider k="camFps" min={5} max={30} unit="fps" /></Row>
      <div style={{ marginTop: 16 }}>
        <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, letterSpacing: '1.4px', textTransform: 'uppercase', marginBottom: 10 }}>카메라 목록</div>
        {['cam_02','cam_03','cam_07','cam_11','cam_12','cam_19','cam_22'].map(cam => (
          <div key={cam} style={{ display: 'flex', alignItems: 'center', gap: 10, padding: '8px 0', borderTop: `1px solid ${C.border}` }}>
            <span style={{ width: 6, height: 6, borderRadius: '50%', background: '#22C55E', flexShrink: 0 }} />
            <span style={{ fontFamily: C.mono, fontSize: 10, color: C.fg2, flex: 1 }}>{cam}</span>
            <span style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4 }}>ONLINE · {vals.camFps}fps</span>
          </div>
        ))}
      </div>
    </>);

    if (active === 'realtime') return (<>
      <Row label="Realtime 활성화"              sub="Supabase Realtime 연결"><Toggle k="realtimeEnabled" /></Row>
      <Row label="incident_created push"        sub="새 incident 실시간 반영"><Toggle k="realtimePushNew" /></Row>
      <Row label="incident_status_updated push" sub="상태 변경 실시간 반영"><Toggle k="realtimePushStatus" /></Row>
      <div style={{ marginTop: 20, padding: '14px', background: C.bg3, border: `1px solid ${C.border}`, borderRadius: C.R }}>
        <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, marginBottom: 8 }}>REALTIME STATUS</div>
        <div style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
          <span style={{ width: 6, height: 6, borderRadius: '50%', background: vals.realtimeEnabled ? '#22C55E' : C.fg4, animation: vals.realtimeEnabled ? 'pulse 2s infinite' : 'none' }} />
          <span style={{ fontFamily: C.mono, fontSize: 10, color: vals.realtimeEnabled ? '#22C55E' : C.fg4 }}>{vals.realtimeEnabled ? 'CONNECTED' : 'DISCONNECTED'}</span>
        </div>
      </div>
    </>);

    if (active === 'system') return (<>
      <Row label="운영자 ID" sub="현재 세션 operator_id">
        <input value={vals.operatorId} onChange={e => set('operatorId', e.target.value)}
          style={{ background: C.bg3, color: C.fg2, border: `1px solid ${C.border2}`, borderRadius: C.R, padding: '5px 10px', fontFamily: C.mono, fontSize: 10, outline: 'none', width: 140 }} />
      </Row>
      <Row label="사이트" sub="현재 운영 사이트">
        <Select k="site" options={[{ value: 'factory_A', label: 'factory_A' }, { value: 'factory_B', label: 'factory_B' }]} />
      </Row>
      <Row label="타임존" sub="시각 표시 기준">
        <Select k="timezone" options={[{ value: 'Asia/Seoul', label: 'Asia/Seoul' }, { value: 'UTC', label: 'UTC' }]} />
      </Row>
      <div style={{ marginTop: 20 }}>
        <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, letterSpacing: '1.4px', textTransform: 'uppercase', marginBottom: 12 }}>API 엔드포인트</div>
        {[
          { label: 'Incident List',    val: 'GET /api/incidents' },
          { label: 'Incident Detail',  val: 'GET /api/incidents/{id}' },
          { label: 'Status Change',    val: 'PATCH /api/incidents/{id}/status' },
          { label: 'AI Generate',      val: 'POST /api/incidents/{id}/ai/generate' },
          { label: 'Status History',   val: 'GET /api/incidents/{id}/status-history' },
        ].map((e, i) => (
          <div key={i} style={{ display: 'flex', justifyContent: 'space-between', padding: '7px 0', borderTop: `1px solid ${C.border}` }}>
            <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg4 }}>{e.label}</span>
            <span style={{ fontFamily: C.mono, fontSize: 9, color: C.fg2 }}>{e.val}</span>
          </div>
        ))}
      </div>
    </>);

    if (active === 'debug') return (<>
      <Row label="디버그 모드"     sub="상세 로그 및 개발자 도구 활성화"><Toggle k="debugMode" /></Row>
      <Row label="API 호출 표시"   sub="각 API 요청/응답을 콘솔에 출력"><Toggle k="showApiCalls" /></Row>
      <Row label="로그 레벨"       sub="출력할 최소 로그 레벨">
        <Select k="logLevel" options={[{ value: 'debug', label: 'DEBUG' }, { value: 'info', label: 'INFO' }, { value: 'warn', label: 'WARN' }, { value: 'error', label: 'ERROR' }]} />
      </Row>
      <Row label="Mock API 지연"   sub="가짜 API 응답 지연 시간 (ms)"><Slider k="mockDelay" min={0} max={3000} step={100} unit="ms" /></Row>
      <div style={{ marginTop: 20, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {[
          { label: 'RESET INCIDENTS',      icon: 'refresh-cw', color: C.fg3 },
          { label: 'CLEAR AI CACHE',       icon: 'trash-2',    color: C.fg3 },
          { label: 'FORCE REALTIME SYNC',  icon: 'radio',      color: C.orange },
          { label: 'EXPORT DEBUG LOG',     icon: 'download',   color: C.fg3 },
        ].map((btn, i) => (
          <button key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, background: 'transparent', border: `1px solid ${C.border2}`, borderRadius: '999px', padding: '7px 14px', fontFamily: C.mono, fontSize: 8.5, color: btn.color, letterSpacing: '0.8px', textTransform: 'uppercase', cursor: 'pointer' }}>
            <Icon name={btn.icon} size={11} color={btn.color} /> {btn.label}
          </button>
        ))}
      </div>
      {vals.debugMode && (
        <div style={{ marginTop: 16, background: C.bg3, border: `1px solid ${C.border}`, borderRadius: C.R, padding: '12px 14px', fontFamily: C.mono, fontSize: 9, color: '#22C55E', lineHeight: 1.8 }}>
          <div style={{ color: C.fg4, marginBottom: 6 }}>// DEBUG CONSOLE</div>
          <div>[INFO] Realtime: {vals.realtimeEnabled ? 'connected' : 'disconnected'}</div>
          <div>[INFO] Site: {vals.site} · Operator: {vals.operatorId}</div>
          <div>[INFO] Mock delay: {vals.mockDelay}ms</div>
          <div>[INFO] Log level: {vals.logLevel.toUpperCase()}</div>
          <div style={{ color: C.orange }}>[WARN] Running in mock mode — no real backend</div>
        </div>
      )}
    </>);

    return null;
  };

  return (
    <div style={{ flex: 1, display: 'flex', overflow: 'hidden' }}>
      {/* Settings sidebar */}
      <div style={{ width: 200, borderRight: `1px solid ${C.border}`, display: 'flex', flexDirection: 'column', flexShrink: 0 }}>
        <div style={{ padding: '18px 18px 12px', borderBottom: `1px solid ${C.border}` }}>
          <div style={{ fontFamily: C.sans, fontSize: 15, color: C.fg1 }}>Settings</div>
          <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, marginTop: 3 }}>시스템 구성 및 디버그</div>
        </div>
        <nav style={{ flex: 1, padding: '8px' }}>
          {SECTIONS.map(s => (
            <div key={s.id} onClick={() => setActive(s.id)}
              style={{ display: 'flex', alignItems: 'center', gap: 9, padding: '8px 10px', marginBottom: 1, cursor: 'pointer', borderRadius: C.R, background: active === s.id ? C.orangeD : 'transparent', transition: 'background 120ms' }}>
              <Icon name={s.icon} size={13} color={active === s.id ? C.orange : C.fg4} />
              <span style={{ fontFamily: C.sans, fontSize: 12, color: active === s.id ? C.fg1 : C.fg3 }}>{s.label}</span>
            </div>
          ))}
        </nav>
      </div>
      {/* Settings content */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '24px 36px' }}>
        <div style={{ maxWidth: 560 }}>
          <div style={{ fontFamily: C.mono, fontSize: 8.5, color: C.fg4, letterSpacing: '1.6px', textTransform: 'uppercase', marginBottom: 20, paddingBottom: 10, borderBottom: `1px solid ${C.border}` }}>
            {SECTIONS.find(s => s.id === active)?.label}
          </div>
          {renderContent()}
        </div>
      </div>
    </div>
  );
}
