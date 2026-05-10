import { describe, it, expect } from 'vitest';
import { formatDueDate, toDateInputValue } from './dateUtils.ts';

describe('dateUtils', () => {
  describe('formatDueDate', () => {
    it('should return "No due date" for null input', () => {
      expect(formatDueDate(null)).toBe('No due date');
    });

    it('should return "No due date" for undefined input', () => {
      expect(formatDueDate(undefined)).toBe('No due date');
    });

    it('should extract date part from ISO datetime string', () => {
      expect(formatDueDate('2026-05-10T14:30:00Z')).toBe('2026-05-10');
    });

    it('should handle date-only strings', () => {
      expect(formatDueDate('2026-05-10')).toBe('2026-05-10');
    });

    it('should return original string if no T separator found', () => {
      expect(formatDueDate('2026-05-10 14:30:00')).toBe('2026-05-10 14:30:00');
    });

    it('should handle empty string before T separator', () => {
      const result = formatDueDate('T14:30:00');
      expect(result).toBe('T14:30:00');
    });
  });

  describe('toDateInputValue', () => {
    it('should return empty string for null input', () => {
      expect(toDateInputValue(null)).toBe('');
    });

    it('should return empty string for undefined input', () => {
      expect(toDateInputValue(undefined)).toBe('');
    });

    it('should extract date part from ISO datetime string', () => {
      expect(toDateInputValue('2026-05-10T14:30:00Z')).toBe('2026-05-10');
    });

    it('should handle date-only strings', () => {
      expect(toDateInputValue('2026-05-10')).toBe('2026-05-10');
    });

    it('should return original string if no T separator found', () => {
      expect(toDateInputValue('2026-05-10 14:30:00')).toBe('2026-05-10 14:30:00');
    });

    it('should handle various ISO formats', () => {
      expect(toDateInputValue('2026-12-31T23:59:59.999Z')).toBe('2026-12-31');
      expect(toDateInputValue('2026-01-01T00:00:00+00:00')).toBe('2026-01-01');
    });
  });
});
