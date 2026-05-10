/**
 * Formats a date string for display.
 * @param dueDate - ISO date string or null/undefined
 * @returns Formatted date string (YYYY-MM-DD) or 'No due date'
 */
export const formatDueDate = (dueDate?: string | null): string => {
  if (!dueDate) {
    return 'No due date';
  }

  const datePart = dueDate.split('T')[0];
  return datePart || dueDate;
};

/**
 * Converts a date string to a value suitable for date input fields.
 * @param dueDate - ISO date string or null/undefined
 * @returns Date string in YYYY-MM-DD format or empty string
 */
export const toDateInputValue = (dueDate?: string | null): string => {
  if (!dueDate) {
    return '';
  }

  return dueDate.split('T')[0] || dueDate;
};
