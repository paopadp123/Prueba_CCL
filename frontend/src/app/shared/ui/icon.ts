import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

interface IconShape {
  paths: string[];
  circles?: [number, number, number][];
}

/**
 * Íconos de trazo dibujados en una grilla de 24×24. Se pintan con `currentColor`,
 * así toman el color del texto que los rodea.
 */
const icons = {
  home: { paths: ['M3 11l9-8 9 8', 'M5 10v10h5v-6h4v6h5V10'] },
  box: { paths: ['M3 7.5L12 3l9 4.5-9 4.5-9-4.5z', 'M3 7.5v9L12 21l9-4.5v-9', 'M12 12v9'] },
  entry: { paths: ['M14 3h5a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-5', 'M3 12h12', 'M10 7l5 5-5 5'] },
  exit: { paths: ['M10 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h5', 'M9 12h12', 'M16 7l5 5-5 5'] },
  chart: { paths: ['M3 3v18h18', 'M7 15l4-4 3 3 6-7'] },
  history: { paths: ['M12 7v5l3 2'], circles: [[12, 12, 9]] },
  search: { paths: ['M20 20l-4-4'], circles: [[11, 11, 7]] },
  logout: { paths: ['M10 3H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h5', 'M9 12h12', 'M16 7l5 5-5 5'] },
  menu: { paths: ['M4 6h16', 'M4 12h16', 'M4 18h16'] },
  close: { paths: ['M6 6l12 12', 'M18 6L6 18'] },
  chevronLeft: { paths: ['M15 18l-6-6 6-6'] },
  chevronRight: { paths: ['M9 18l6-6-6-6'] },
  chevronDown: { paths: ['M6 9l6 6 6-6'] },
  arrowLeft: { paths: ['M19 12H5', 'M11 6l-6 6 6 6'] },
  arrowRight: { paths: ['M5 12h14', 'M13 6l6 6-6 6'] },
  alert: { paths: ['M12 3l9.5 17h-19L12 3z', 'M12 10v4', 'M12 17.5v.01'] },
  plus: { paths: ['M12 5v14', 'M5 12h14'] },
  edit: { paths: ['M4 20h4L19 9l-4-4L4 16v4z', 'M13 7l4 4'] },
  stock: { paths: ['M4 20V10', 'M10 20V4', 'M16 20v-7', 'M22 20H2'] },
  sun: {
    paths: ['M12 2v2', 'M12 20v2', 'M4.9 4.9l1.4 1.4', 'M17.7 17.7l1.4 1.4', 'M2 12h2', 'M20 12h2', 'M4.9 19.1l1.4-1.4', 'M17.7 6.3l1.4-1.4'],
    circles: [[12, 12, 4]],
  },
  monitor: { paths: ['M3 5h18v11H3z', 'M8 20h8', 'M12 16v4'] },
  moon: { paths: ['M20 14.5A8 8 0 0 1 9.5 4a8 8 0 1 0 10.5 10.5z'] },
  eye: { paths: ['M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12z'], circles: [[12, 12, 3]] },
  eyeOff: {
    paths: [
      'M3 3l18 18',
      'M10.6 6.1A9.8 9.8 0 0 1 12 6c6.5 0 10 6 10 6a17 17 0 0 1-3.2 3.9',
      'M6.6 6.6C3.8 8.3 2 12 2 12s3.5 7 10 7a9.6 9.6 0 0 0 5.4-1.6',
      'M9.9 9.9a3 3 0 0 0 4.2 4.2',
    ],
  },
} satisfies Record<string, IconShape>;

export type IconName = keyof typeof icons;

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'icon', 'aria-hidden': 'true' },
  template: `
    <svg
      viewBox="0 0 24 24"
      [attr.width]="size()"
      [attr.height]="size()"
      fill="none"
      stroke="currentColor"
      stroke-width="1.8"
      stroke-linecap="round"
      stroke-linejoin="round"
    >
      @for (path of shape().paths; track $index) {
        <path [attr.d]="path" />
      }
      @for (circle of shape().circles ?? []; track $index) {
        <circle [attr.cx]="circle[0]" [attr.cy]="circle[1]" [attr.r]="circle[2]" />
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
      flex-shrink: 0;
    }
  `,
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(20);

  protected readonly shape = computed<IconShape>(() => icons[this.name()]);
}
