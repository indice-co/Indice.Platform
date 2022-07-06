const enablePurge = process.env.ENABLE_PURGE || true;
const colors = require('tailwindcss/colors')

module.exports = {
    mode: 'jit',
    purge: {
        enabled: enablePurge,
        content: [
            './src/**/*.{html,ts,css,scss}',
            './node_modules/@indice/ng-components/_styles.css'
        ]
    },
    theme: {
        extend: {
            colors: {
                'sky': colors.sky
                // "gray": {
                //     "50": "#F8F9FC",
                //     "100": "#DDE4EE",
                //     "200": "#A9B8D1",
                //     "300": "#788DB0",
                //     "400": "#546887",
                //     "500": "#374151",
                //     "600": "#2F3A4B",
                //     "700": "#293547",
                //     "800": "#222D3F",
                //     "900": "#1D283A"
                // }
            }
        },
    },
    variants: {
        extend: {
            border: ['focus'],
            opacity: ['disabled']
        }
    },
    plugins: [
        require('@tailwindcss/typography'),
        require('@tailwindcss/forms'),
        require('@tailwindcss/line-clamp'),
        require('@tailwindcss/aspect-ratio')
    ],
};
