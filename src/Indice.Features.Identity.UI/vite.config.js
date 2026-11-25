import { defineConfig } from 'vite';
import { viteStaticCopy } from 'vite-plugin-static-copy';
import path from 'path';
import { fileURLToPath } from 'url';
import { globSync } from 'glob';
import autoprefixer from 'autoprefixer';
import { readFileSync } from 'node:fs';

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
// CSS masks work with server-rendered and Knockout-created content, without an icon runtime.
const lucideIcons = [
    'arrow-left', 'arrow-right', 'camera', 'calendar', 'check', 'chevron-down',
    'chevron-right', 'circle', 'circle-alert', 'circle-check', 'circle-help',
    'circle-minus', 'clock', 'contact', 'download', 'ellipsis-vertical', 'external-link',
    'eye', 'eye-off', 'fingerprint', 'key-round', 'lock-keyhole', 'log-out', 'mail',
    'message-square', 'phone', 'refresh-cw', 'server', 'shield-check', 'shield-off',
    'smartphone', 'square-asterisk', 'square-pen', 'triangle-alert', 'user-round', 'x'
];
const lucideStyles = () => ({
    name: 'identity-lucide-icons',
    generateBundle() {
        const masks = lucideIcons.map(name => {
            const svg = readFileSync(path.resolve(__dirname, `node_modules/lucide-static/icons/${name}.svg`), 'utf8');
            return `--idui-icon-${name}: url("data:image/svg+xml,${encodeURIComponent(svg)}");`;
        }).join('\n');
        const classes = lucideIcons.map(name => `.idui-icon--${name} { mask-image: var(--idui-icon-${name}); }`).join('\n');
        this.emitFile({
            type: 'asset',
            fileName: 'css/lucide.css',
            source: `/* Generated from lucide-static. License: ../lib/lucide/LICENSE */\n:root {\n${masks}\n}\n.idui-icon {
    display: inline-block;
    flex-shrink: 0;
    width: 1em;
    height: 1em;
    vertical-align: -.15em;
    background-color: currentColor;
    mask-position: center;
    mask-size: contain;
    mask-repeat: no-repeat;
}\n${classes}\n@media (forced-colors: active) {
    .idui-icon {
        forced-color-adjust: none;
        background-color: CanvasText;
    }
}\n`
        });
    }
});
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
                    path.resolve(__dirname, 'node_modules')
                ]
            }
        },
        postcss: {
            plugins: [
                autoprefixer()
            ]
        }
    },
    plugins: [
        lucideStyles(),
        viteStaticCopy({
            targets: [
                // Copy Bootstrap
                {
                    src: 'node_modules/bootstrap/dist/js/bootstrap.bundle.min.js',
                    dest: 'lib/bootstrap/dist/js',
                    transform: stripComments
                },
                {
                    src: 'node_modules/bootstrap/LICENSE',
                    dest: 'lib/bootstrap/dist/js',
                    rename: 'LICENSE.md'
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
                {
                    src: 'node_modules/jquery/LICENSE.txt',
                    dest: 'lib/jquery/dist'
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
                {
                    src: 'node_modules/jquery-validation/LICENSE.md',
                    dest: 'lib/jquery-validation/dist'
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
                {
                    src: 'node_modules/jquery-validation-unobtrusive/LICENSE.txt',
                    dest: 'lib/jquery-validation-unobtrusive/dist'
                },
                // Copy Knockout
                {
                    src: 'node_modules/knockout/build/output/knockout-latest.js',
                    dest: 'lib/knockout/build/output',
                    transform: stripComments
                },
                {
                    src: 'node_modules/knockout/LICENSE',
                    dest: 'lib/knockout/build/output',
                    rename: 'LICENSE.md'
                },
                // Copy Knockout Secure Binding
                {
                    src: 'node_modules/knockout-secure-binding/dist/knockout-secure-binding.min.js',
                    dest: 'lib/knockout-secure-binding/dist',
                    transform: stripComments
                },
                {
                    src: 'node_modules/knockout-secure-binding/LICENSE',
                    dest: 'lib/knockout-secure-binding/dist',
                    rename: 'LICENSE.md'
                },
                // Copy Popper.js
                {
                    src: 'node_modules/@popperjs/core/dist/umd/popper.min.js',
                    dest: 'lib/@popperjs/core/dist/umd',
                    transform: stripComments
                },
                {
                    src: 'node_modules/@popperjs/core/LICENSE.md',
                    dest: 'lib/@popperjs/core/dist/umd'
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
                {
                    src: 'node_modules/@fingerprintjs/fingerprintjs/LICENSE',
                    dest: 'lib/@fingerprintjs/fingerprintjs/dist',
                    rename: 'LICENSE.md'
                },
                {
                    src: 'node_modules/lucide-static/LICENSE',
                    dest: 'lib/lucide'
                },
                // Copy Swiper
                {
                    src: 'node_modules/swiper/swiper-bundle.min.js',
                    dest: 'lib/swiper',
                    transform: stripComments
                },
                {
                    src: 'node_modules/swiper/LICENSE',
                    dest: 'lib/swiper',
                    rename: 'LICENSE.md'
                },
                {
                    src: 'node_modules/swiper/swiper-bundle.min.css',
                    dest: 'lib/swiper'
                },
                // Copy qrcodejs2
                {
                    src: 'node_modules/qrcodejs2/qrcode.min.js',
                    dest: 'lib/qrcodejs2',
                    transform: stripComments
                },
                {
                    src: 'node_modules/qrcodejs2/LICENSE',
                    dest: 'lib/qrcodejs2',
                    rename: 'LICENSE.md'
                }
            ],
            silent: false
        })
    ]
});
