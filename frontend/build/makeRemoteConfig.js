const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const { ModuleFederationPlugin } = require('webpack').container;

// Shared Webpack 5 Module Federation config factory for the portal remotes.
module.exports = function makeRemoteConfig({ dirname, name, port, deps }) {
  return (_, argv) => {
    const isProd = argv.mode === 'production';
    return {
      entry: './src/index.ts',
      mode: isProd ? 'production' : 'development',
      devtool: isProd ? false : 'eval-source-map',
      output: { publicPath: `http://localhost:${port}/`, clean: true },
      resolve: {
        extensions: ['.tsx', '.ts', '.jsx', '.js'],
        alias: { '@urp/shared': path.resolve(dirname, '../../shared/src') },
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
        port,
        historyApiFallback: true,
        hot: true,
        headers: { 'Access-Control-Allow-Origin': '*' },
      },
      plugins: [
        new ModuleFederationPlugin({
          name,
          filename: 'remoteEntry.js',
          exposes: { './App': './src/App' },
          shared: {
            react: { singleton: true, requiredVersion: deps.react },
            'react-dom': { singleton: true, requiredVersion: deps['react-dom'] },
          },
        }),
        new HtmlWebpackPlugin({ template: './public/index.html' }),
      ],
    };
  };
};
