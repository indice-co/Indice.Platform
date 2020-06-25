'use strict';

var gulp = require('gulp'),
    sass = require('gulp-sass'),
    del = require('del'),
    cssbeautify = require('gulp-cssbeautify'),
    npmDist = require('gulp-npm-dist')

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
