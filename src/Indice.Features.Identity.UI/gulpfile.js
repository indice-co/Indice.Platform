'use strict';
import { src, dest, watch, task, series } from "gulp";
import * as dartSass from 'sass'
import gulpSass from 'gulp-sass';
import { deleteAsync } from 'del';
import cssbeautify from "gulp-cssbeautify";
import npmDist from "gulp-npm-dist";

import cleanCSS from "gulp-clean-css";
import stripCssComments from "gulp-strip-css-comments";
import rename from "gulp-rename";
import postcss from 'gulp-postcss';
import vars from "postcss-simple-vars";
import nested from "postcss-nested";
import mixins from 'postcss-mixins';
import precss from 'precss';
import tailwindcss from 'tailwindcss';
import autoprefixer from 'autoprefixer';
import twconfig from './tailwind.config.js'
import postcssSass from '@csstools/postcss-sass';

const sass = gulpSass(dartSass);

var webroot = './wwwroot/',
    lib = './wwwroot/lib/';

task('sass-bootstrap', function () {
    return src([webroot + 'css/**/*.scss', '!' + webroot + 'css/identity.tw.scss'])
        .pipe(sass().on('error', sass.logError))
        .pipe(cssbeautify())
        .pipe(dest(webroot + 'css/'));
});

task('sass:watch', function () {
    watch([webroot + 'css/**/*.scss', '!' + webroot + 'css/identity.tw.scss'], gulp.series('sass-bootstrap'));
});

task('clean:lib', function (cb) {
    deleteAsync([
        lib + '**'
    ]).then(function () {
        cb();
    });
});

task('copy:libs', async function () {
    // result should not be empty, otherwise the task will crash
    if (npmDist().length === 0) {
        return;
    }
    return src(npmDist(), { base: './node_modules' })
               .pipe(dest(lib));
});

/* tailwind specific */
function tailwind(cb) {
    src(webroot + 'css/identity.tw.scss')
        .pipe(stripCssComments({ preserve: false }))
        .pipe(postcss([
            postcssSass,
            mixins,
            vars,
            nested,
            precss,
            tailwindcss(twconfig),
            autoprefixer
        ], { includePaths: ["node_modules"] }))
        .pipe(rename({ extname: '.css' }))
        .pipe(cleanCSS({ level: 2 }))
        .pipe(dest(webroot + 'css/'));
    cb();
};


function tailwind_watch(cb) {
    watch(webroot + 'css/**/*.scss', tailwind);
    cb();
}

task('tailwind', tailwind);
task('tailwind:watch', tailwind_watch);

task('sass', series('sass-bootstrap', 'tailwind'));