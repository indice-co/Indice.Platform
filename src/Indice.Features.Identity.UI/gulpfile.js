'use strict';

import gulp from "gulp";
import dartSass from 'sass';
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

gulp.task('sass-bootstrap', function () {
    return gulp.src([webroot + 'css/**/*.scss', '!' + webroot + 'css/identity.tw.scss'])
        .pipe(sass().on('error', sass.logError))
        .pipe(cssbeautify())
        .pipe(gulp.dest(webroot + 'css/'));
});

gulp.task('sass:watch', function () {
    gulp.watch([webroot + 'css/**/*.scss', '!' + webroot + 'css/identity.tw.scss'], gulp.series('sass-bootstrap'));
});

gulp.task('clean:lib', function (cb) {
    deleteAsync([
        lib + '**'
    ]).then(function () {
        cb();
    });
});

gulp.task('copy:libs', function () {
    return gulp.src(npmDist(), {
        base: './node_modules'
    })
    .pipe(gulp.dest(lib));
});

/* tailwind specific */
gulp.task('tailwind', () => {
    return gulp.src(webroot + 'css/identity.tw.scss')
        .pipe(postcss([
            postcssSass,
            mixins,
            vars,
            nested,
            precss,
            tailwindcss(twconfig),
            autoprefixer
        ]))
        .pipe(rename({
            extname: '.css'
        }))
        .pipe(stripCssComments({ preserve: false }))
        .pipe(cleanCSS({ level: 2 }))
        .pipe(gulp.dest(webroot + 'css/'));
});

gulp.task('tailwind:watch', function () {
    gulp.watch(webroot + 'css/**/*.scss', gulp.series('tailwind'));
});

gulp.task('sass', gulp.series('sass-bootstrap', 'tailwind'));