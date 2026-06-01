import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';

import { routes } from './app.routes';

const Preset = definePreset(Aura, {
  semantic: {
    colorScheme: {
      light: {
        content: { borderColor: '#bebebe' },
        formField: { borderColor: '#bebebe' },
      },
    },
  },
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    provideAnimationsAsync(),
    providePrimeNG({
      ripple: true,
      theme: {
        preset: Preset,
        options: {
          darkModeSelector: '.dark-mode',
          prefix: 'p',
          cssLayer: false,
        },
      },
    }),
  ],
};
