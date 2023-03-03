'use strict';

import gulp from "gulp";
import dartSass from 'sass';
import gulpSass from 'gulp-sass';
import del from "del";
import cssbeautify from "gulp-cssbeautify";
import npmDist from "gulp-npm-dist";
const sass = gulpSass(dartSass);

var webroot = './wwwroot/',
    lib = './wwwroot/lib/';

gulp.task('sass', function () {
    return gulp.src(webroot + 'css/**/*.scss')
        .pipe(sass().on('error', sass.logError))
        .pipe(cssbeautify())
        .pipe(gulp.dest(webroot + 'css/'));
});

gulp.task('sass:watch', function () {
    gulp.watch(webroot + 'css/**/*.scss', gulp.series('sass'));
});

gulp.task('clean:lib', function (cb) {
    del([
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
