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
      publicPath: 'http://localhost:3000/',
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
      new ModuleFederationPlugin({
        name: 'shell',
        remotes: {
          employee: 'employee@http://localhost:3001/remoteEntry.js',
          manager: 'manager@http://localhost:3002/remoteEntry.js',
          hr: 'hr@http://localhost:3003/remoteEntry.js',
          finance: 'finance@http://localhost:3004/remoteEntry.js',
        },
        shared: {
          react: { singleton: true, requiredVersion: deps.react },
          'react-dom': { singleton: true, requiredVersion: deps['react-dom'] },
        },
      }),
      new HtmlWebpackPlugin({ template: './public/index.html' }),
    ],
  };
};
