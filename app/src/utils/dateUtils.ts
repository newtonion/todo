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

/**
 * Checks if a date is in the past or within the next two days.
 * @param soonestDueDate - ISO date string, Date object, or null/undefined
 * @returns True if the date is in the past or within the next two days, false otherwise
 */
export function isPastOrWithinNextTwoDays(
  soonestDueDate: string | Date | null | undefined
): boolean {
  if (!soonestDueDate) return false;

  const due = new Date(soonestDueDate);
  if (Number.isNaN(due.getTime())) return false;

  const now = new Date();
  const twoDaysFromNow = new Date(now);
  twoDaysFromNow.setDate(now.getDate() + 2);

  return due <= twoDaysFromNow;
}

/**
 * Checks if a date is in the past.
 * @param soonestDueDate - ISO date string, Date object, or null/undefined
 * @returns True if the date is before today, false otherwise
 */
export function isPast(
  soonestDueDate: string | Date | null | undefined
): boolean {
  if (!soonestDueDate) return false;

  const due = new Date(soonestDueDate);
  if (Number.isNaN(due.getTime())) return false;

  const now = new Date();
  const dueDateOnly = new Date(due.getFullYear(), due.getMonth(), due.getDate());
  const todayDateOnly = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  return dueDateOnly < todayDateOnly;
}
