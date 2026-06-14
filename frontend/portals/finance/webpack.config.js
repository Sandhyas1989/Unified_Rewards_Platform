module.exports = require('../../build/makeRemoteConfig')({
  dirname: __dirname,
  name: 'finance',
  port: 3004,
  deps: require('./package.json').dependencies,
});
