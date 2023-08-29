# IdentityUI.Assets

This project containes all client side assets needed to run the Identity UI. 
It is built as a separate project in order to avoid the RazorClassLibrary staticweb assets bundling that takes place by default.
Here the assets are included in the dll instead of the package and are delivered throuh a composite file provider via StaticFileMiddleware.
This makes it easy to override a single file in the host web project like the logo.svg without triggering a compilation error.

## TODO: 
Consider changing the default task runner (bundler, scss transpiler) from gulp.js to web pack reading the following article
https://medium.com/@luylucas10/asp-net-core-mvc-webpack-and-vue-js-update-f10fcc76238c