console.log('hello');
const dgram = require('node:dgram');
const net = require('node:net');
const server = dgram.createSocket('udp4');

server.on('error', (err) => {
  console.error(`server error:\n${err.stack}`);
  server.close();
});

server.on('message', (msg, sender) => {
  var datetime = (new Date()).getTime();
  var clientToServerPing = datetime - parseInt(msg);

  var jason = {
    "clientToServerPing":clientToServerPing,
    "serverdatetime":datetime};
  // console.log(`clientToServerPing: ${clientToServerPing}`);

  // console.log(`datetime: ${datetime}`);
  // console.log(`server got: ${msg} from ${sender.address}:${sender.port}`);
  server.send(JSON.stringify(jason), sender.port, sender.address, (err) => {
    console.log(`Message sent to ${sender.address}:${sender.port}`)
  })
});

server.on('listening', () => {
  const address = server.address();
  console.log(`server listening ${address.address}:${address.port}`);
});

server.bind(41234);
//Prints: server listening 0.0.0.0:41234


// const server = net.createServer((c) => {
//   // 'connection' listener.
//   console.log('client connected');
//   c.on('end', () => {
//     console.log('client disconnected');
//   });
//   c.write('hello\r\n');
//   c.pipe(c);
// });
// server.on('error', (err) => {
//   throw err;
// });
// server.listen(8124, () => {
//   console.log('server bound');
// })
