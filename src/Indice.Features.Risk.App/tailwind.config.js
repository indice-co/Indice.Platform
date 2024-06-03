const enablePurge = process.env.ENABLE_PURGE || true;
const colors = require('tailwindcss/colors')

module.exports = {
  mode: 'jit',
  content: [
    './src/**/*.{html,ts,css,scss}',
    './node_modules/@indice/ng-components/_styles.css'
  ],
  theme: {
    extend: {
      border: ['focus'],
      opacity: ['disabled'],
      colors: {
        'sky': colors.sky,
        'dark-green': '#03292e',
        'darker-green': '#124046',
        'dusty-orange': '#f27731',
        'dusty-orange-opacity': '#f2773199'
      }
    }
  },
  variants: {},
  plugins: [
    require('@tailwindcss/typography'),
    require('@tailwindcss/forms'),
    require('@tailwindcss/aspect-ratio')
  ],
};
