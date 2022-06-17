const enablePurge = process.env.ENABLE_PURGE || true;

module.exports = {
    mode: 'jit',
    content: [
        './src/**/*.{html,ts,css,scss}',
        './node_modules/@indice/ng-components/_styles.css'
    ],
    theme: {
        extend: {},
    },
    variants: {},
    plugins: [
        require('@tailwindcss/typography'),
        require('@tailwindcss/forms'),
        require('@tailwindcss/line-clamp'),
        require('@tailwindcss/aspect-ratio')
    ],
};
