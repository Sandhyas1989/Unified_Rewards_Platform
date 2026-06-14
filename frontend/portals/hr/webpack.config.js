module.exports = require('../../build/makeRemoteConfig')({
  dirname: __dirname,
  name: 'hr',
  port: 3003,
  deps: require('./package.json').dependencies,
});
