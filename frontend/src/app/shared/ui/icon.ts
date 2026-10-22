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
