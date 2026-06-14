module.exports = require('../../build/makeRemoteConfig')({
  dirname: __dirname,
  name: 'manager',
  port: 3002,
  deps: require('./package.json').dependencies,
});
