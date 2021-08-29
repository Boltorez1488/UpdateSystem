const mix = require('webpack-mix');

/*
 |--------------------------------------------------------------------------
 | Mix Asset Management
 |--------------------------------------------------------------------------
 |
 | Mix provides a clean, fluent API for defining some Webpack build steps
 | for your Laravel application. By default, we are compiling the Sass
 | file for the application as well as bundling up all the JS files.
 |
 */

mix.setPublicPath('/');

const path = 'js';//'../wwwroot/js';

if (!mix.inProduction()) {
    mix.webpackConfig({ devtool: "inline-source-map", node: { fs: 'empty' } });
    mix.js('dev/js/app.js', path).sourceMaps();
    mix.js('dev/js/welcome.js', path).sourceMaps();
} else {
    mix.js('dev/js/app.js', path);
    mix.js('dev/js/welcome.js', path);
}
