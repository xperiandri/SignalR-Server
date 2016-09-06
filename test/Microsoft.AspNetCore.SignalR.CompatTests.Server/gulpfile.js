const gulp = require('gulp');
const ts = require('gulp-typescript');
const del = require('del');
const gulpTypings = require('gulp-typings');

gulp.task('prepare:copylibs', ['prepare:clean-output'], function () {
    return gulp.src([
        './node_modules/signalr/jquery.signalR{.min.js,.js}',
        './node_modules/jquery/dist/jquery{.min.js,.js}',
        './node_modules/jasmine-core/lib/jasmine-core/boot.js',
        './node_modules/jasmine-core/lib/jasmine-core/jasmine.css',
        './node_modules/jasmine-core/lib/jasmine-core/jasmine.js',
        './node_modules/jasmine-core/lib/jasmine-core/jasmine-html.js',
    ]).pipe(gulp.dest('wwwroot/lib'));
});

gulp.task('prepare:typings', function () {
    return gulp.src('./typings.json')
        .pipe(gulpTypings());
});

gulp.task('prepare:clean-output', function () {
    return del([
        'wwwroot/js',
        'wwwroot/lib'
    ]);
});

gulp.task('prepare', ['prepare:clean-output', 'prepare:typings', 'prepare:copylibs']);

gulp.task('compile:typescript', ['prepare'], function () {
    gulp.src(['jstests/**/*.ts', 'typings/**/*.d.ts'])
        .pipe(ts({
            out: 'tests.js',
            noEmitOnError: true
        }))
        .pipe(gulp.dest('wwwroot/js'));
});

gulp.task('compile', ['compile:typescript']);

gulp.task('default', ['compile'], function () {
    // place code for your default task here
});