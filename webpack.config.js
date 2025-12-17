const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {
    mode: 'development', // Use 'production' for minified output
    entry: {
        app: [
            './ClientApp/js/app.js',
            './ClientApp/css/app.css'
        ]
    },
    output: {
        path: path.resolve(__dirname, 'WebApplication1/wwwroot/dist'),
        filename: 'bundle.js'
    },
    module: {
        rules: [
            {
                test: /\.css$/,
                use: [MiniCssExtractPlugin.loader, 'css-loader']
            }
        ]
    },
    plugins: [
        new MiniCssExtractPlugin({
            filename: 'bundle.css'
        })
    ]
};
