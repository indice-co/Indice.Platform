const enablePurge = process.env.ENABLE_PURGE || true;
const colors = require('tailwindcss/colors')

module.exports = {
  mode: 'jit',
  purge: {
    enabled: enablePurge,
    content: [
      './src/**/*.{html,ts,css,scss}',
      './node_modules/@indice/ng-components/_styles.scss'
    ]
  },
  theme: {
    extend: {
      colors: {
        'sky': colors.sky,
        'dark-green': '#03292e',
        'darker-green': '#124046',
        'dusty-orange': '#f27731',
        'dusty-orange-opacity': '#f2773199'
      }
    },
  },
  darkMode: 'class',
  variants: {
    extend: {
      border: ['focus'],
      opacity: ['disabled']
    }
  },
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
    require('@tailwindcss/aspect-ratio')
  ],
};
