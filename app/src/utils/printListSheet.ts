import type { ListPrintItemResult, ListPrintResult } from '../api/lists/models';

const escapeHtml = (value: string) => value
  .replace(/&/g, '&amp;')
  .replace(/</g, '&lt;')
  .replace(/>/g, '&gt;')
  .replace(/"/g, '&quot;')
  .replace(/'/g, '&#39;');

const renderPrintItem = (item: ListPrintItemResult, depth = 0): string => {
  const subItems = item.subItems.length > 0
    ? `<div class="sub-items">${item.subItems.map((subItem) => renderPrintItem(subItem, depth + 1)).join('')}</div>`
    : '';

  return `
    <section class="task-item depth-${depth}">
      <div class="task-line">
        <span class="checkbox">${item.isCompleted ? '&#10003;' : ''}</span>
        <span class="task-name">${escapeHtml(item.name)}</span>
      </div>
      ${subItems}
    </section>
  `;
};

export const buildPrintableListHtml = (list: ListPrintResult) => `
<!doctype html>
<html>
  <head>
    <title>${escapeHtml(list.name)} print sheet</title>
    <style>
      * {
        box-sizing: border-box;
      }

      body {
        margin: 0;
        color: #111827;
        background: #ffffff;
        font-family: Arial, sans-serif;
      }

      main {
        max-width: 8in;
        margin: 0 auto;
        padding: 0.5in;
      }

      h1 {
        margin: 0 0 0.3in;
        font-size: 24pt;
        font-weight: 700;
      }

      .task-item {
        break-inside: avoid;
      }

      .task-line {
        display: grid;
        grid-template-columns: 0.28in minmax(0, 1fr) auto;
        gap: 0.12in;
        align-items: center;
        min-height: 0.36in;
        padding: 0.08in 0;
        border-bottom: 1px solid #d1d5db;
      }

      .checkbox {
        display: inline-flex;
        width: 0.22in;
        height: 0.22in;
        align-items: center;
        justify-content: center;
        border: 2px solid #111827;
        font-size: 12pt;
        font-weight: 700;
        line-height: 1;
      }

      .task-name {
        font-size: 13pt;
        overflow-wrap: anywhere;
      }

      .due-date {
        color: #4b5563;
        font-size: 10pt;
        white-space: nowrap;
      }

      .sub-items {
        margin-left: 0.34in;
      }

      .depth-1 .task-name {
        font-size: 12pt;
      }

      @media print {
        main {
          padding: 0;
        }
      }
    </style>
  </head>
  <body>
    <main>
      <h1>${escapeHtml(list.name)}</h1>
      ${list.items.map((item) => renderPrintItem(item)).join('')}
    </main>
  </body>
</html>
`;

export const writePrintableListSheet = (targetWindow: Window, list: ListPrintResult) => {
  const printSheetUrl = URL.createObjectURL(new Blob(
    [buildPrintableListHtml(list)],
    { type: 'text/html' }
  ));

  targetWindow.location.replace(printSheetUrl);
  window.setTimeout(() => URL.revokeObjectURL(printSheetUrl), 60_000);
};
