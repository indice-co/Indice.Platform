import flowbite from 'flowbite/plugin.js';
export default {
    content: ['./Pages/Tailwind/**/*.cshtml', '**/*.scss',
        './node_modules/flowbite/**/*.js'],
    theme: {
        extend: {
            screens: {
                '3xl': '1600px',
                '4xl': '1920px',
                'xs': '475px',
            },
            backgroundImage: {
                'hero-image': 'var(--tenant-bg-image)',
            },
            width: {
                'fill': '-webkit-fill-available'
            },
            height: {
                'fill': '-webkit-fill-available'
            },
            colors: {
                'tenant': {
                    900: 'var(--tenant900)',
                    800: 'var(--tenant800)',
                    700: 'var(--tenant700)',
                    600: 'var(--tenant600)',
                    500: 'var(--tenant500)',
                    400: 'var(--tenant400)',
                },
                'greys': {
                    900: '#1D1D1B',
                    800: '#3C3C3B',
                    700: '#9E9E9D',
                    600: '#B1B1B0',
                    500: '#BBBBBA',
                    400: '#CECECD',
                    300: '#DEDEDE',
                    200: '#EBEBEB',
                    100: '#F5F5F5',
                },
                'one': '#24314D',
                'black': '#000000',
                'white': '#ffffff',
                'danger': {
                    900: '#F34F4E',
                    800: '#F88F8E',
                    700: '#FFF2F2',
                },
                'info': {
                    900: '#3150EC',
                    800: '#7E91F3',
                    700: '#F3F3FF',
                },
                'success': {
                    900: '#25BF60',
                    800: '#72D599',
                    700: '#E7F8F0',
                },
                'warning': {
                    900: '#FFC107',
                    800: '#FFD75F',
                    700: '#FFF8E5',
                },
            }
        },
    },
    plugins: [
        flowbite
    ],
    variants: {
    },
}