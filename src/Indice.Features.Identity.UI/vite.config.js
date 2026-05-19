import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import path from 'path';
import { fileURLToPath } from 'url';
import { globSync } from 'glob';
import tailwindcss from 'tailwindcss';
import autoprefixer from 'autoprefixer';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Find all main SCSS entry files (not partials)
const scssFiles = globSync('./wwwroot/css/**/*.scss', {
    ignore: './wwwroot/css/**/_*.scss'
});

// Convert to Vite input format
const scssInputs = {};
scssFiles.forEach(file => {
    const relativePath = path.relative('./wwwroot', file);
    const name = relativePath.replace(/\.scss$/, '').replace(/\\/g, '/');
    scssInputs[name] = file;
});
console.log('SCSS entry points:', scssInputs);
const stripComments = (contents) => contents.toString().replace(/\/\*[\s\S]*?\*\/|^\s*\/\/.*/gm, '');
export default defineConfig({
    root: './',
    base: './',
    publicDir: false,
    build: {
        outDir: './wwwroot',
        emptyOutDir: false,
        manifest: false,
        assetsInlineLimit: 0,
        rollupOptions: {
            input: scssInputs,
            output: {
                assetFileNames: (assetInfo) => {
                    // Keep static files and generated CSS files in their original locations
                    if (assetInfo.name.endsWith('.css')) {
                        // Extract the original path from the input names
                        let cssName = assetInfo.name.replace(/\.css$/, '');
                        if (assetInfo.originalFileName) {
                            const relativePath = path.relative('./wwwroot', assetInfo.originalFileName || assetInfo.name).replace(/\\/g, '/');
                            cssName = relativePath.replace(/\.scss$/, '');
                        }
                        return `${cssName}.css`;
                    }
                    const relativePath = path.relative('./wwwroot', assetInfo.originalFileName || assetInfo.name).replace(/\\/g, '/');
                    return relativePath;
                }
            }
        },
        cssCodeSplit: true,
        sourcemap: false,
        minify: false,
        target: 'es2015',
        write: true
    },
    esbuild: {
        legalComments: 'none'
    },
    css: {
        devSourcemap: false,
        preprocessorOptions: {
            scss: {
                api: 'modern',
                silenceDeprecations: ['legacy-js-api', 'mixed-decls', 'color-functions', 'global-builtin', 'import'],
                quietDeps: true,
                loadPaths: [
                    path.resolve(__dirname, 'node_modules'),
                    path.resolve(__dirname, 'wwwroot/css')
                ]
            }
        },
        postcss: {
            plugins: [
                tailwindcss,
                autoprefixer()
            ]
        }
    },
    plugins: [
        viteStaticCopy({
            targets: [
                // Copy Bootstrap
                {
                    src: 'node_modules/bootstrap/dist/js/bootstrap.bundle.min.js',
                    dest: 'lib/bootstrap/dist/js',
                    transform: stripComments
                },
                {
                    src: 'node_modules/bootstrap/dist/css/bootstrap.min.css',
                    dest: 'lib/bootstrap/dist/css'
                },
                // Copy Bootstrap.Native
                {
                    src: 'node_modules/bootstrap.native/dist/bootstrap-native.js',
                    dest: 'lib/bootstrap.native/dist',
                    transform: stripComments
                },
                // Copy jQuery
                {
                    src: 'node_modules/jquery/dist/jquery.min.js',
                    dest: 'lib/jquery/dist',
                    transform: stripComments
                },
                // Copy jQuery Validation
                {
                    src: 'node_modules/jquery-validation/dist/jquery.validate.min.js',
                    dest: 'lib/jquery-validation/dist',
                    transform: stripComments
                },
                {
                    src: 'node_modules/jquery-validation/dist/additional-methods.min.js',
                    dest: 'lib/jquery-validation/dist',
                    transform: stripComments
                },
                // Copy jQuery Validation Unobtrusive (with parseJSON fix)
                {
                    src: 'node_modules/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js',
                    dest: 'lib/jquery-validation-unobtrusive/dist',
                    transform: (contents) => {

                        // Replace deprecated $.parseJSON with native JSON.parse

                        return stripComments(contents).replace(/(\w|\$)\.parseJSON/g, 'JSON.parse');
                    }
                },
                // Copy Knockout
                {
                    src: 'node_modules/knockout/build/output/knockout-latest.js',
                    dest: 'lib/knockout/build/output',
                    transform: stripComments
                },
                // Copy Knockout Secure Binding
                {
                    src: 'node_modules/knockout-secure-binding/dist/knockout-secure-binding.min.js',
                    dest: 'lib/knockout-secure-binding/dist',
                    transform: stripComments
                },
                // Copy Popper.js
                {
                    src: 'node_modules/@popperjs/core/dist/umd/popper.min.js',
                    dest: 'lib/@popperjs/core/dist/umd',
                    transform: stripComments
                },
                // Copy SignalR
                {
                    src: 'node_modules/@microsoft/signalr/dist/browser/signalr.min.js',
                    dest: 'lib/@microsoft/signalr/dist/browser',
                    transform: stripComments
                },
                // Copy FingerprintJS
                {
                    src: 'node_modules/@fingerprintjs/fingerprintjs/dist/fp.min.js',
                    dest: 'lib/@fingerprintjs/fingerprintjs/dist',
                    transform: stripComments
                },
                // Copy Font Awesome 4.7
                {
                    src: 'node_modules/font-awesome/css/font-awesome.min.css',
                    dest: 'lib/font-awesome/css'
                },
                {
                    src: 'node_modules/font-awesome/fonts/*',
                    dest: 'lib/font-awesome/fonts'
                },
                // Copy FontAwesome Free
                {
                    src: 'node_modules/@fortawesome/fontawesome-free/css/all.min.css',
                    dest: 'lib/@fortawesome/fontawesome-free/css'
                },
                {
                    src: 'node_modules/@fortawesome/fontawesome-free/webfonts/*',
                    dest: 'lib/@fortawesome/fontawesome-free/webfonts'
                },
                // Copy Swiper
                {
                    src: 'node_modules/swiper/swiper-bundle.min.js',
                    dest: 'lib/swiper',
                    transform: stripComments
                },
                {
                    src: 'node_modules/swiper/swiper-bundle.min.css',
                    dest: 'lib/swiper'
                },
                // Copy Flowbite
                {
                    src: 'node_modules/flowbite/dist/flowbite.min.js',
                    dest: 'lib/flowbite/dist',
                    transform: stripComments
                },
                {
                    src: 'node_modules/flowbite/dist/flowbite.min.css',
                    dest: 'lib/flowbite/dist'
                },
                // Copy qrcodejs2
                {
                    src: 'node_modules/qrcodejs2/qrcode.min.js',
                    dest: 'lib/qrcodejs2',
                    transform: stripComments
                }
            ],
            silent: false
        })
    ]
});
