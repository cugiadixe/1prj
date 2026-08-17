import React from 'react';
import { Empty } from 'antd';

// Bảng màu phân loại — tương phản tốt, đọc rõ trên nền sáng.
export const PALETTE = [
  '#3b82f6', '#22c55e', '#f59e0b', '#8b5cf6', '#ef4444',
  '#06b6d4', '#ec4899', '#84cc16', '#f97316', '#14b8a6',
  '#6366f1', '#eab308',
];

const AXIS = '#94a3b8';
const GRID = '#eef2f6';
const TEXT = '#475569';

export interface Datum {
  label: string;
  value: number;
}

const fmtInt = (n: number) => n.toLocaleString('vi-VN');

export const fmtCompactVnd = (n: number): string => {
  if (n >= 1_000_000_000) return `${(n / 1_000_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} tỷ`;
  if (n >= 1_000_000) return `${(n / 1_000_000).toLocaleString('vi-VN', { maximumFractionDigits: 1 })} tr`;
  if (n >= 1_000) return `${(n / 1_000).toLocaleString('vi-VN', { maximumFractionDigits: 0 })} k`;
  return fmtInt(n);
};

const isEmpty = (data: Datum[]) => data.length === 0 || data.every((d) => d.value === 0);

const EmptyBox: React.FC = () => (
  <div style={{ display: 'flex', justifyContent: 'center', padding: '28px 0' }}>
    <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Chưa có dữ liệu" />
  </div>
);

/** Chọn giá trị trục "đẹp" (bội của 1/2/5 × 10^k) lớn hơn max. */
function niceMax(max: number): number {
  if (max <= 0) return 1;
  const pow = Math.pow(10, Math.floor(Math.log10(max)));
  const n = max / pow;
  const step = n <= 1 ? 1 : n <= 2 ? 2 : n <= 5 ? 5 : 10;
  return step * pow;
}

// ---------- Donut ----------
export const DonutChart: React.FC<{ data: Datum[]; unit?: string }> = ({ data, unit }) => {
  if (isEmpty(data)) return <EmptyBox />;
  const total = data.reduce((s, d) => s + d.value, 0);
  const size = 180;
  const r = 70;
  const cx = size / 2;
  const cy = size / 2;
  const circ = 2 * Math.PI * r;
  let offset = 0;

  return (
    <div style={{ display: 'flex', gap: 16, alignItems: 'center', flexWrap: 'wrap' }}>
      <svg viewBox={`0 0 ${size} ${size}`} width={size} height={size} style={{ flexShrink: 0 }}>
        <g transform={`rotate(-90 ${cx} ${cy})`}>
          {data.map((d, i) => {
            const frac = d.value / total;
            const dash = frac * circ;
            const el = (
              <circle
                key={d.label}
                cx={cx}
                cy={cy}
                r={r}
                fill="none"
                stroke={PALETTE[i % PALETTE.length]}
                strokeWidth={22}
                strokeDasharray={`${dash} ${circ - dash}`}
                strokeDashoffset={-offset}
              />
            );
            offset += dash;
            return el;
          })}
        </g>
        <text x={cx} y={cy - 4} textAnchor="middle" fontSize={26} fontWeight={700} fill="#1e293b">
          {fmtInt(total)}
        </text>
        <text x={cx} y={cy + 16} textAnchor="middle" fontSize={12} fill={TEXT}>
          {unit ?? 'Tổng'}
        </text>
      </svg>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 6, minWidth: 140, flex: 1 }}>
        {data.map((d, i) => (
          <div key={d.label} style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 13 }}>
            <span style={{ width: 10, height: 10, borderRadius: 3, background: PALETTE[i % PALETTE.length], flexShrink: 0 }} />
            <span style={{ color: TEXT, flex: 1 }}>{d.label}</span>
            <span style={{ fontWeight: 600, color: '#1e293b' }}>{fmtInt(d.value)}</span>
            <span style={{ color: AXIS, width: 42, textAlign: 'right' }}>
              {total ? Math.round((d.value / total) * 100) : 0}%
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};

// ---------- Cột dọc ----------
export const BarChart: React.FC<{ data: Datum[]; color?: string; valueFormatter?: (n: number) => string }> = ({
  data,
  color = PALETTE[0],
  valueFormatter = fmtInt,
}) => {
  if (isEmpty(data)) return <EmptyBox />;
  const W = 520;
  const H = 240;
  const padL = 44;
  const padR = 12;
  const padT = 16;
  const padB = 40;
  const plotW = W - padL - padR;
  const plotH = H - padT - padB;
  const max = niceMax(Math.max(...data.map((d) => d.value)));
  const n = data.length;
  const band = plotW / n;
  const barW = Math.min(46, band * 0.6);
  const ticks = 4;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" preserveAspectRatio="xMidYMid meet">
      {Array.from({ length: ticks + 1 }).map((_, i) => {
        const y = padT + (plotH * i) / ticks;
        const val = max - (max * i) / ticks;
        return (
          <g key={i}>
            <line x1={padL} y1={y} x2={W - padR} y2={y} stroke={GRID} />
            <text x={padL - 8} y={y + 4} textAnchor="end" fontSize={11} fill={AXIS}>
              {valueFormatter(Math.round(val))}
            </text>
          </g>
        );
      })}
      {data.map((d, i) => {
        const h = max ? (d.value / max) * plotH : 0;
        const x = padL + band * i + (band - barW) / 2;
        const y = padT + plotH - h;
        return (
          <g key={d.label}>
            <rect x={x} y={y} width={barW} height={h} rx={4} fill={color} />
            {d.value > 0 && (
              <text x={x + barW / 2} y={y - 5} textAnchor="middle" fontSize={11} fontWeight={600} fill={TEXT}>
                {valueFormatter(d.value)}
              </text>
            )}
            <text x={x + barW / 2} y={H - padB + 16} textAnchor="middle" fontSize={11} fill={TEXT}>
              {d.label.length > 10 ? `${d.label.slice(0, 9)}…` : d.label}
            </text>
          </g>
        );
      })}
    </svg>
  );
};

// ---------- Cột ngang ----------
export const HBarChart: React.FC<{ data: Datum[] }> = ({ data }) => {
  if (isEmpty(data)) return <EmptyBox />;
  const max = Math.max(...data.map((d) => d.value));
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      {data.map((d, i) => (
        <div key={d.label} style={{ display: 'flex', alignItems: 'center', gap: 10, fontSize: 13 }}>
          <span style={{ width: 120, color: TEXT, textAlign: 'right', flexShrink: 0 }}>{d.label}</span>
          <div style={{ flex: 1, background: GRID, borderRadius: 6, height: 18, position: 'relative' }}>
            <div
              style={{
                width: `${max ? (d.value / max) * 100 : 0}%`,
                background: PALETTE[i % PALETTE.length],
                height: '100%',
                borderRadius: 6,
                minWidth: d.value > 0 ? 4 : 0,
                transition: 'width .3s',
              }}
            />
          </div>
          <span style={{ width: 48, fontWeight: 600, color: '#1e293b', textAlign: 'right' }}>{fmtInt(d.value)}</span>
        </div>
      ))}
    </div>
  );
};

// ---------- Vùng / đường ----------
export const AreaChart: React.FC<{ data: Datum[]; color?: string; valueFormatter?: (n: number) => string }> = ({
  data,
  color = PALETTE[0],
  valueFormatter = fmtInt,
}) => {
  if (data.length === 0) return <EmptyBox />;
  const W = 520;
  const H = 240;
  const padL = 48;
  const padR = 14;
  const padT = 16;
  const padB = 34;
  const plotW = W - padL - padR;
  const plotH = H - padT - padB;
  const max = niceMax(Math.max(1, ...data.map((d) => d.value)));
  const n = data.length;
  const stepX = n > 1 ? plotW / (n - 1) : 0;
  const px = (i: number) => padL + stepX * i;
  const py = (v: number) => padT + plotH - (max ? (v / max) * plotH : 0);
  const line = data.map((d, i) => `${px(i)},${py(d.value)}`).join(' ');
  const area = `${padL},${padT + plotH} ${line} ${px(n - 1)},${padT + plotH}`;
  const ticks = 4;
  const gid = `area-grad-${color.replace('#', '')}`;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} width="100%" preserveAspectRatio="xMidYMid meet">
      <defs>
        <linearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity={0.28} />
          <stop offset="100%" stopColor={color} stopOpacity={0.02} />
        </linearGradient>
      </defs>
      {Array.from({ length: ticks + 1 }).map((_, i) => {
        const y = padT + (plotH * i) / ticks;
        const val = max - (max * i) / ticks;
        return (
          <g key={i}>
            <line x1={padL} y1={y} x2={W - padR} y2={y} stroke={GRID} />
            <text x={padL - 8} y={y + 4} textAnchor="end" fontSize={11} fill={AXIS}>
              {valueFormatter(Math.round(val))}
            </text>
          </g>
        );
      })}
      <polygon points={area} fill={`url(#${gid})`} />
      <polyline points={line} fill="none" stroke={color} strokeWidth={2.5} strokeLinejoin="round" strokeLinecap="round" />
      {data.map((d, i) => (
        <g key={d.label}>
          <circle cx={px(i)} cy={py(d.value)} r={3.5} fill="#fff" stroke={color} strokeWidth={2} />
          <text x={px(i)} y={H - padB + 16} textAnchor="middle" fontSize={11} fill={TEXT}>
            {d.label}
          </text>
        </g>
      ))}
    </svg>
  );
};
