'use strict';

var gulp        = require('gulp'),
    del         = require('del'),
    sass        = require('gulp-sass'),
    cssbeautify = require('gulp-cssbeautify'),
    npmDist     = require('gulp-npm-dist');

var lib = './wwwroot/lib/';

gulp.task('sass', function () {
    return gulp.src('./wwwroot/css/*.scss')
               .pipe(sass().on('error', sass.logError))
               .pipe(cssbeautify())
               .pipe(gulp.dest('./wwwroot/css/'));
});

gulp.task('sass:watch', function () {
    gulp.watch('./wwwroot/**/*.scss', ['sass']);
});

gulp.task('clean:lib', function (cb) {
    del([
        lib + '**'
    ]).then(function () { cb(); });
});

gulp.task('copy:libs', function () {
    gulp.src(npmDist(), { base: './node_modules' })
        .pipe(gulp.dest(lib));
});
