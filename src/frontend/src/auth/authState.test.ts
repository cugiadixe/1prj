import { describe, it, expect, beforeEach } from 'vitest';
import {
  getAuthState,
  setAuthState,
  clearAuthState,
} from './authState';

describe('authState — in-memory token storage', () => {
  beforeEach(() => {
    clearAuthState();
  });

  it('starts unauthenticated with no access token', () => {
    const state = getAuthState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.user).toBeNull();
  });

  it('setAuthState stores access token in memory', () => {
    setAuthState('test-token', false, { userId: 1, username: 'u1', displayName: null });
    const state = getAuthState();
    expect(state.accessToken).toBe('test-token');
    expect(state.isAuthenticated).toBe(true);
  });

  it('access token is not written to localStorage', () => {
    setAuthState('test-token', false, null);
    expect(localStorage.getItem('accessToken')).toBeNull();
    expect(localStorage.getItem('access_token')).toBeNull();
    expect(localStorage.getItem('token')).toBeNull();
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)!;
      expect(key.toLowerCase()).not.toContain('token');
      expect(key.toLowerCase()).not.toContain('auth');
    }
  });

  it('access token is not written to sessionStorage', () => {
    setAuthState('test-token', false, null);
    expect(sessionStorage.getItem('accessToken')).toBeNull();
    expect(sessionStorage.getItem('access_token')).toBeNull();
    expect(sessionStorage.getItem('token')).toBeNull();
    for (let i = 0; i < sessionStorage.length; i++) {
      const key = sessionStorage.key(i)!;
      expect(key.toLowerCase()).not.toContain('token');
      expect(key.toLowerCase()).not.toContain('auth');
    }
  });

  it('setAuthState stores mustChangePassword flag', () => {
    setAuthState('tok', true, null);
    expect(getAuthState().mustChangePassword).toBe(true);
  });

  it('clearAuthState resets all fields', () => {
    setAuthState('tok', true, { userId: 1, username: 'u', displayName: null });
    clearAuthState();
    const state = getAuthState();
    expect(state.accessToken).toBeNull();
    expect(state.isAuthenticated).toBe(false);
    expect(state.mustChangePassword).toBe(false);
    expect(state.user).toBeNull();
  });
});
