const gulp = require('gulp');
const ts = require('gulp-typescript');
const del = require('del');
const gulpTypings = require('gulp-typings');

gulp.task('prepare:typings', function () {
    return gulp.src('./typings.json')
        .pipe(gulpTypings());
});

gulp.task('prepare:clean-output', function () {
    return del([
        'wwwroot/js'
    ]);
});

gulp.task('prepare', ['prepare:clean-output', 'prepare:typings']);

gulp.task('compile:typescript', ['prepare'], function () {
    gulp.src(['wwwroot/compatTests/*.ts', 'typings/**/*.d.ts'])
        .pipe(ts({
            out: 'compatTests.js',
            noEmitOnError: true,
            target: "ES5"
        }))
        .pipe(gulp.dest('wwwroot/js'));
});

gulp.task('compile', ['compile:typescript']);

gulp.task('default', ['compile'], function () {
    // place code for your default task here
});