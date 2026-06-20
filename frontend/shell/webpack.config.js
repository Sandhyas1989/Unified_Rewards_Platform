const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const { ModuleFederationPlugin } = require('webpack').container;
const deps = require('./package.json').dependencies;

module.exports = (_, argv) => {
  const isProd = argv.mode === 'production';
  return {
    entry: './src/index.ts',
    mode: isProd ? 'production' : 'development',
    devtool: isProd ? false : 'eval-source-map',
    output: {
      // Local dev serves from :3000; production is served from the Static Web App root.
      publicPath: isProd ? '/' : 'http://localhost:3000/',
      clean: true,
    },
    resolve: {
      extensions: ['.tsx', '.ts', '.jsx', '.js'],
      alias: { '@urp/shared': path.resolve(__dirname, '../shared/src') },
    },
    module: {
      rules: [
        {
          test: /\.[jt]sx?$/,
          exclude: /node_modules/,
          use: {
            loader: 'babel-loader',
            options: {
              presets: [
                '@babel/preset-env',
                ['@babel/preset-react', { runtime: 'automatic' }],
                '@babel/preset-typescript',
              ],
            },
          },
        },
        { test: /\.css$/, use: ['style-loader', 'css-loader'] },
      ],
    },
    devServer: {
      port: 3000,
      historyApiFallback: true,
      hot: true,
      headers: { 'Access-Control-Allow-Origin': '*' },
    },
    plugins: [
      // Portals are bundled directly into the shell (see src/App.tsx) for a single-origin
      // static deployment, so no Module Federation / remote dev servers are needed.
      new HtmlWebpackPlugin({ template: './public/index.html' }),
    ],
  };
};
