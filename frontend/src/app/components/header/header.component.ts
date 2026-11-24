import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  template: `
    <header class="app-header">
      <div class="header-inner">

        <!-- Brand / Logo -->
        <div class="header-left">
          <button class="mobile-nav-toggle" (click)="toggleMobileDrawer()" title="Open Navigation Menu">
            <span>☰</span>
          </button>

          <a routerLink="/dashboard" class="brand">
            <div class="brand-logo-wrap">
              <img src="assets/logo.png" alt="PFC" class="brand-logo" />
            </div>
            <div class="brand-text">
              <span class="brand-name">PUKALE FURNISHING CENTRE</span>
              <span class="brand-sub">Furnishing &amp; Interior CRM</span>
            </div>
          </a>
        </div>

        <!-- Desktop Navigation Bar -->
        <div class="nav-section-label">WORKSPACE</div>
        <nav class="nav-links desktop-nav">
          <a routerLink="/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
            <span class="nav-icon">📊</span>
            <span>Dashboard</span>
          </a>
          <a routerLink="/quotation-maker" routerLinkActive="active" class="nav-link-highlight" target="_blank" rel="noopener">
            <span class="nav-icon">🧾</span>
            <span>New Quotation</span>
          </a>
          <a routerLink="/calculation" routerLinkActive="active">
            <span class="nav-icon">📐</span>
            <span>Calculator</span>
          </a>
          <a routerLink="/customers" routerLinkActive="active">
            <span class="nav-icon">👥</span>
            <span>Customers</span>
          </a>
          <a routerLink="/products" routerLinkActive="active">
            <span class="nav-icon">📦</span>
            <span>Orders &amp; Stages</span>
          </a>
          <a routerLink="/quotations" routerLinkActive="active">
            <span class="nav-icon">📋</span>
            <span>Quotations</span>
          </a>
          <a routerLink="/monthly-report" routerLinkActive="active">
            <span class="nav-icon">📈</span>
            <span>Reports</span>
          </a>
          <a routerLink="/settings" routerLinkActive="active">
            <span class="nav-icon">⚙️</span>
            <span>Settings</span>
          </a>
        </nav>

        <a routerLink="/quotation-maker" class="sidebar-create desktop-create" target="_blank" rel="noopener"><span>＋</span><span>New quotation</span></a>

        <!-- Right Side: User Profile & Sign Out -->
        <div class="header-right">
          <!-- User Chip -->
          <div class="user-chip">
            <div class="user-avatar">{{ getUserInitial() }}</div>
            <span class="user-name">{{ auth.getUsername() || 'Admin' }}</span>
            <span class="user-status-dot" title="Active"></span>
          </div>

          <!-- Sign Out -->
          <button class="btn-logout" (click)="auth.logout()" title="Sign out of system">
            <span>Sign out</span>
            <span class="logout-icon">↗</span>
          </button>
        </div>

      </div>
    </header>

    <!-- Mobile Drawer Overlay & Panel -->
    <div class="mobile-drawer-overlay" *ngIf="showMobileDrawer" (click)="closeMobileDrawer()">
      <div class="mobile-drawer" (click)="$event.stopPropagation()">
        <div class="mobile-drawer-header">
          <div class="brand" style="gap:10px;">
            <div class="brand-logo-wrap" style="width:34px; height:34px;">
              <img src="assets/logo.png" alt="PFC" class="brand-logo" />
            </div>
            <div class="brand-text">
                <span class="brand-name" style="font-size:0.8rem;">PUKALE FURNISHING CENTRE</span>
              <span class="brand-sub">Navigation Menu</span>
            </div>
          </div>
          <button class="btn-close" (click)="closeMobileDrawer()">✕</button>
        </div>

        <div class="mobile-drawer-links">
          <a routerLink="/dashboard" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">📊</span>
            <span>Dashboard</span>
          </a>
          <a routerLink="/quotation-maker" routerLinkActive="active" (click)="closeMobileDrawer()" target="_blank" rel="noopener">
            <span class="nav-icon">🧾</span>
            <span>New Quotation</span>
          </a>
          <a routerLink="/calculation" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">📐</span>
            <span>Window Calculator</span>
          </a>
          <a routerLink="/customers" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">👥</span>
            <span>Customer Directory</span>
          </a>
          <a routerLink="/products" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">📦</span>
            <span>Orders &amp; Work Stages</span>
          </a>
          <a routerLink="/quotations" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">📋</span>
            <span>Quotations Archive</span>
          </a>
          <a routerLink="/monthly-report" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">📈</span>
            <span>Monthly Reports</span>
          </a>
          <a routerLink="/settings" routerLinkActive="active" (click)="closeMobileDrawer()">
            <span class="nav-icon">⚙️</span>
            <span>Settings</span>
          </a>
        </div>

        <div class="mobile-drawer-footer">
          <div style="display:flex; align-items:center; justify-content:space-between; margin-bottom:12px;">
            <div style="display:flex; align-items:center; gap:8px;">
              <div class="user-avatar" style="width:32px; height:32px;">{{ getUserInitial() }}</div>
              <span style="font-size:0.85rem; font-weight:700; color:var(--text-1);">{{ auth.getUsername() || 'Admin' }}</span>
            </div>
            <span class="user-status-dot"></span>
          </div>
          <button class="btn btn-secondary btn-sm" style="width:100%; justify-content:center;" (click)="auth.logout()">
            <span>Sign out</span>
            <span>↗</span>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .app-header { background:rgba(255,255,255,.92); border-right:1px solid rgba(0,0,0,.08); position:fixed; inset:0 auto 0 0; z-index:100; width:244px; box-shadow:none; backdrop-filter:blur(18px); }
    .header-inner {
      max-width:none; margin:0; display:flex; align-items:stretch; flex-direction:column;
      height:100vh; padding:25px 16px 16px; gap:0;
    }
    .header-left {
      display: flex;
      align-items: center;
      gap: 12px;
      width:100%;
      min-width:0;
    }
    .mobile-nav-toggle {
      display: none;
      background: transparent;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 6px 10px;
      font-size: 1.1rem;
      cursor: pointer;
      color: #334155;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 10px;
      text-decoration: none;
      flex-shrink: 0;
      min-width:0;
      flex:1;
    }
    .brand:hover { opacity: 0.95; }
    .brand-logo-wrap {
      width: 36px;
      height: 36px;
      border-radius: 9px;
      overflow: hidden;
      border: 1px solid #e2e8f0;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #09090b;
      flex-shrink: 0;
    }
    .brand-logo {
      width: 100%;
      height: 100%;
      object-fit: contain;
    }
    .brand-text {
      display: flex;
      flex-direction: column;
      line-height: 1.2;
      min-width:0;
      flex:1;
    }
    .brand-name {
      font-size: 0.73rem;
      font-weight: 800;
      letter-spacing: 0.025em;
      color: #1d1d1f;
      line-height:1.25;
      white-space:normal;
      overflow-wrap:anywhere;
    }
    .brand-sub {
      font-size: 0.68rem;
      color: #6e6e73;
      font-weight: 600;
    }

    .desktop-nav {
      display: flex;
      align-items: stretch;
      flex-direction: column;
      gap: 4px;
      margin-top: 12px;
    }
    .nav-links a {
      padding: 10px 12px;
      border-radius: 9px;
      text-decoration: none;
      color: #475569;
      font-size: 0.86rem;
      font-weight: 600;
      transition: all 0.15s ease;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
    .nav-links a:hover {
      background: #f1f5f9;
      color: #0f172a;
    }
    .nav-links a.active {
      background: #f5f5f7;
      color: #1d1d1f;
      font-weight: 700;
      box-shadow: inset 2px 0 #0071e3;
    }
    .nav-links a.active .nav-icon {
      transform: scale(1.05);
    }
    .nav-link-highlight {
      color: #334155 !important;
      font-weight: 600 !important;
    }
    .nav-link-highlight:hover {
      background: #eef2ff !important;
    }
    .ext-tag {
      font-size: 0.72rem;
      opacity: 0.75;
    }
    .nav-icon {
      font-size: 0.95rem;
      line-height: 1;
      width:20px; text-align:center;
    }

    .header-right {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      flex-shrink: 0;
      flex-direction:column; border-top:1px solid #edf0f3; padding-top:14px; margin-top:auto;
    }
    .user-chip {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 4px 0;
      background: #fff;
      border-radius: 10px;
      border: 0;
    }
    .user-avatar {
      width: 26px;
      height: 26px;
      border-radius: 50%;
      background: linear-gradient(135deg, #4f46e5 0%, #3730a3 100%);
      color: #ffffff;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 0.75rem;
      font-weight: 800;
    }
    .user-name {
      font-size: 0.8rem;
      color: #1e293b;
      font-weight: 700;
    }
    .user-status-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #10b981;
    }

    .btn-logout {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      padding: 6px 11px;
      border-radius: 8px;
      cursor: pointer;
      color: #64748b;
      font-size: 0.78rem;
      font-family: inherit;
      font-weight: 600;
      transition: all 0.15s;
      display: inline-flex;
      align-items: center;
      gap: 4px;
    }
    .btn-logout:hover {
      background: #fff1f2;
      color: #e11d48;
      border-color: #fecdd3;
    }
    .logout-icon { font-size: 0.78rem; }

    /* Mobile Drawer */
    .mobile-drawer-overlay {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.5);
      backdrop-filter: blur(4px);
      z-index: 200;
      display: flex;
    }
    .mobile-drawer {
      width: 280px;
      max-width: 85vw;
      background: #ffffff;
      height: 100%;
      display: flex;
      flex-direction: column;
      box-shadow: 4px 0 24px rgba(0,0,0,0.15);
      animation: slideIn 0.2s ease-out;
    }
    @keyframes slideIn {
      from { transform: translateX(-100%); }
      to { transform: translateX(0); }
    }
    .mobile-drawer-header {
      padding: 16px;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .mobile-drawer-links {
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 4px;
      flex: 1;
      overflow-y: auto;
    }
    .mobile-drawer-links a {
      padding: 10px 14px;
      border-radius: 8px;
      text-decoration: none;
      color: #334155;
      font-size: 0.88rem;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .mobile-drawer-links a:hover {
      background: #f1f5f9;
    }
    .mobile-drawer-links a.active {
      background: #0f172a;
      color: #ffffff;
      font-weight: 700;
    }
    .mobile-drawer-footer {
      padding: 16px;
      border-top: 1px solid #e2e8f0;
      background: #f8fafc;
    }

    .sidebar-create { display:flex; align-items:center; justify-content:center; gap:8px; text-decoration:none; background:#0071e3; color:white; border-radius:999px; padding:10px 12px; margin-top:14px; font-size:.86rem; font-weight:600; box-shadow:none; }
    .sidebar-create:hover { background:#0077ed; }
    .sidebar-create span:first-child { font-size:1.1rem; }
    @media (max-width: 1024px) {
      .app-header { position:sticky; inset:auto; width:auto; border-right:0; border-bottom:1px solid #e2e8f0; }
      .header-inner { flex-direction:row; align-items:center; justify-content:space-between; height:62px; padding:0 18px; gap:12px; }
      .header-left { flex-direction:row; }
      .brand-name { white-space:nowrap; font-size:.78rem; }
      .brand { flex:0 1 auto; }
      .brand-text { flex:0 1 auto; }
      .desktop-create { display:none; }
      .nav-section-label { display:none; }
      .header-right { flex-direction:row; align-items:center; border:0; padding:0; margin:0; }
      .desktop-nav { display: none; }
      .mobile-nav-toggle { display: block; }
    }
    @media (max-width: 640px) {
      .brand-sub { display: none; }
      .user-name { display: none; }
      .btn-logout span:first-child { display: none; }
    }
  `]
})
export class HeaderComponent {
  showMobileDrawer = false;

  constructor(public auth: AuthService, private router: Router) {}

  getUserInitial(): string {
    const u = this.auth.getUsername();
    return u ? u.charAt(0).toUpperCase() : 'A';
  }

  toggleMobileDrawer() {
    this.showMobileDrawer = !this.showMobileDrawer;
  }

  closeMobileDrawer() {
    this.showMobileDrawer = false;
  }

  @HostListener('document:keydown.escape')
  onEscape() {
    this.showMobileDrawer = false;
  }
}
