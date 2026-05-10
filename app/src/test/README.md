# Test Suite

This project uses [Vitest](https://vitest.dev/) for unit testing.

## Running Tests

```bash
# Run all tests once
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

## Test Coverage

### Utilities
- **dateUtils.test.ts** - Date formatting and conversion utilities

### Shared Components
- **Modal.test.tsx** - Base modal component with backdrop
- **IconButtons.test.tsx** - Icon button components (Add, Edit, Save, Discard)
- **Toggle.test.tsx** - Checkbox toggle component
- **Pagination.test.tsx** - Pagination navigation component

### List Section Components
- **SortableHeader.test.tsx** - Sortable table header component

## Test Structure

Tests are co-located with their source files:
```
src/
  utils/
    dateUtils.ts
    dateUtils.test.ts
  components/
    shared/
      Modal.tsx
      Modal.test.tsx
```

## Writing Tests

Tests use:
- **Vitest** - Test framework
- **@testing-library/react** - React component testing utilities
- **@testing-library/user-event** - User interaction simulation
- **@testing-library/jest-dom** - DOM matchers
