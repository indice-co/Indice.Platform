import { test, expect } from '@playwright/test';
import { readFile, readdir, mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const themes = ['split', 'panel', 'columns', 'minimal'];
const fixture = await readFile(path.join(root, 'tests/fixtures/identity-ui.html'), 'utf8');
const types = { '.css': 'text/css', '.js': 'text/javascript', '.svg': 'image/svg+xml', '.otf': 'font/otf' };

async function openFixture(page, theme, width) {
    await page.setViewportSize({ width, height: width < 600 ? 844 : 1000 });
    await page.route('http://identity.test/**', async route => {
        const pathname = new URL(route.request().url()).pathname;
        if (pathname === '/') {
            return route.fulfill({ contentType: 'text/html', body: fixture });
        }
        const file = path.join(root, 'wwwroot', pathname);
        await route.fulfill({ contentType: types[path.extname(file)] ?? 'application/octet-stream', body: await readFile(file) });
    });
    await page.goto('http://identity.test/');
    await page.addStyleTag({ content: '* { transition: none !important; }' });
    await page.evaluate(theme => document.documentElement.dataset.theme = theme, theme);
    await page.evaluate(() => document.fonts.ready);
}

async function capture(page, name, testInfo) {
    if (!process.env.IDUI_SCREENSHOT_DIR) return;
    await mkdir(process.env.IDUI_SCREENSHOT_DIR, { recursive: true });
    const file = path.join(process.env.IDUI_SCREENSHOT_DIR, `${name}.png`);
    await page.screenshot({ path: file, fullPage: true });
    await testInfo.attach(name, { path: file, contentType: 'image/png' });
}

async function assertNoOverflow(page) {
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
}

for (const theme of themes) {
    for (const width of [1440, 390]) {
        test(`${theme} at ${width}px: layouts and control states`, async ({ page }, testInfo) => {
            await openFixture(page, theme, width);
            await assertNoOverflow(page);
            const hero = page.locator('.site-hero');
            if (width >= 992 && theme !== 'minimal') {
                await expect(hero).toBeVisible();
                const heroBox = await hero.boundingBox();
                const cardBox = await page.locator('.auth-card').boundingBox();
                expect(heroBox.x + heroBox.width).toBeLessThanOrEqual(cardBox.x + 1);
            } else {
                await expect(hero).toBeHidden();
            }
            await capture(page, `${theme}-${width}-login`, testInfo);

            const button = page.getByRole('button', { name: 'Sign in', exact: true });
            const ink = await button.evaluate(el => getComputedStyle(el).backgroundColor);
            await page.locator('#username').focus();
            await expect(page.locator('#username')).toHaveCSS('border-color', ink);
            await page.locator('#remember').check();
            expect(await page.locator('#remember').evaluate(el => getComputedStyle(el).backgroundColor)).toBe(ink);
            await button.hover();
            await expect(button).not.toHaveCSS('background-color', ink);
            await button.evaluate(el => el.disabled = true);
            await expect(button).toHaveCSS('background-color', ink);
            await expect(button).toBeDisabled();
            await button.evaluate(el => el.disabled = false);

            const reveal = page.locator('.field__reveal');
            const mask = await reveal.evaluate(el => getComputedStyle(el, '::before').maskImage);
            expect(mask).toContain('data:image/svg+xml');
            await reveal.click();
            await expect(page.locator('#password')).toHaveAttribute('type', 'text');
            await expect(reveal).toHaveAttribute('aria-pressed', 'true');
            await expect(reveal).toHaveAccessibleName('Hide password');
            expect(await reveal.evaluate(el => getComputedStyle(el, '::before').maskImage)).not.toBe(mask);
            await page.getByRole('button', { name: 'Hide password' }).click();
            await expect(page.locator('#password')).toHaveAttribute('type', 'password');

            await page.evaluate(() => {
                document.body.classList.replace('login', 'register');
                document.querySelector('#registration-name').hidden = false;
                document.querySelector('h1').textContent = 'Create an account';
                document.querySelector('.auth-form__submit').textContent = 'Create account';
            });
            await assertNoOverflow(page);
            await capture(page, `${theme}-${width}-register`, testInfo);

            await page.evaluate(() => {
                document.querySelector('.alert').hidden = false;
                document.querySelector('#username').classList.add('input-validation-error');
                document.querySelector('#username-error').textContent = 'Enter a valid email address.';
            });
            await expect(page.locator('#username')).toHaveCSS('border-color', 'rgb(210, 35, 42)');
            await expect(page.getByRole('alert')).toBeVisible();
            await assertNoOverflow(page);

            // Masks apply to late-created MFA / profile content, not only the initial DOM.
            await page.evaluate(() => {
                document.body.className = 'authenticated shell-profile';
                document.querySelector('.site-hero').remove();
                document.querySelector('.site-main').innerHTML = `<section class="data-card">
                    <h1 class="data-card__title">Account security</h1>
                    <ul class="choice-list">
                        ${['phone', 'message-square', 'mail', 'shield-check', 'smartphone', 'fingerprint', 'square-asterisk'].map(name =>
                            `<li class="choice-list__item"><span class="choice-list__icon"><i class="idui-icon idui-icon--${name}" aria-hidden="true"></i></span> ${name}</li>`).join('')}
                    </ul>
                    <ul class="password-rules"><li class="password-rules__item">Unmet requirement</li><li class="password-rules__item password-rules__item--valid">Met requirement</li></ul>
                    <button class="btn-close" aria-label="Close"></button>
                </section>`;
            });
            const icons = await page.locator('.choice-list .idui-icon').evaluateAll(elements => elements.map(el => {
                const css = getComputedStyle(el);
                return { mask: css.maskImage, width: el.getBoundingClientRect().width, height: el.getBoundingClientRect().height };
            }));
            for (const icon of icons) {
                expect(icon.mask).toContain('data:image/svg+xml');
                expect(icon.width).toBeGreaterThan(0);
                expect(icon.height).toBeGreaterThan(0);
            }
            for (const selector of ['.password-rules__item', '.password-rules__item--valid']) {
                expect(await page.locator(selector).first().evaluate(el => getComputedStyle(el, '::before').maskImage)).toContain('data:image/svg+xml');
            }
            await assertNoOverflow(page);
            await capture(page, `${theme}-${width}-security`, testInfo);
        });
    }
}

test('all Razor icon mappings are available in the locally built Lucide stylesheet', async () => {
    const css = await readFile(path.join(root, 'wwwroot/css/lucide.css'), 'utf8');
    const files = await readdir(path.join(root, 'Pages/Bootstrap5'), { recursive: true });
    for (const file of files.filter(file => file.endsWith('.cshtml'))) {
        const source = await readFile(path.join(root, 'Pages/Bootstrap5', file), 'utf8');
        expect(source).not.toMatch(/\b(?:fa-(?:solid|regular|brands)|material-icons|ms-Icon)\b/);
        for (const [, name] of source.matchAll(/idui-icon--([a-z-]+)/g)) {
            expect(css, `${file}: missing ${name}`).toContain(`.idui-icon--${name} {`);
        }
    }
});

test('switching a theme recolours existing Bootstrap buttons without reloading', async ({ page }) => {
    await openFixture(page, 'split', 390);
    const colours = [];
    for (const theme of themes) {
        await page.evaluate(theme => document.documentElement.dataset.theme = theme, theme);
        // Disable transitions to measure the final theme, rather than an intermediate colour.
        await page.addStyleTag({ content: '* { transition: none !important; }' });
        colours.push(await page.locator('.btn-primary').evaluate(el => getComputedStyle(el).backgroundColor));
    }
    expect(new Set(colours).size).toBe(themes.length);
});

test('Lucide masks remain painted in Windows high contrast mode', async ({ page }) => {
    await openFixture(page, 'minimal', 390);
    await page.emulateMedia({ forcedColors: 'active' });
    for (const [selector, pseudo] of [['.idui-icon', null], ['.field__reveal', '::before']]) {
        const paint = await page.locator(selector).first().evaluate((el, pseudo) => {
            const css = getComputedStyle(el, pseudo);
            return { background: css.backgroundColor, adjust: css.forcedColorAdjust, mask: css.maskImage };
        }, pseudo);
        expect(paint.mask).toContain('data:image/svg+xml');
        expect(paint.adjust).toBe('none');
        expect(paint.background).not.toBe('rgba(0, 0, 0, 0)');
        expect(paint.background).not.toBe(await page.locator('.auth-card').evaluate(el => getComputedStyle(el).backgroundColor));
    }
});
