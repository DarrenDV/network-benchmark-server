console.log('server starting....');
const dgram = require('node:dgram');
const server = dgram.createSocket('udp4');

var lastPacketID = 0;

server.on('error', (err) => {
  console.error(`server error:\n${err.stack}`);
  server.close();
});

server.on('message', (msg, sender) => {

  var receivedPacket = JSON.parse(msg);

  var clientdatetime = receivedPacket.clientDatetime;

  var datetime = (new Date()).getTime();
  var clientToServerPing = datetime - clientdatetime;

  if(receivedPacket.packetID == 1){ //allows multiple tests without restarting server
    lastPacketID = 0;
  }

  console.log("packetID: " + receivedPacket.packetID);
  console.log("lastPacketID: " + lastPacketID);

  if(receivedPacket.packetID != lastPacketID + 1){




    if(receivedPacket.packetID > lastPacketID + 1){
      console.log("packet went missing");

      var json = {
        "type":"errorPacket",
        "errorType": "missingPacket",
        "packetID":receivedPacket.packetID,
        "clientToServerPing":clientToServerPing,
        "serverDatetime":datetime};
    
      server.send(JSON.stringify(json), sender.port, sender.address, (err) => {
        console.log(`Message sent to ${sender.address}:${sender.port}`)
      })

      lastPacketID = receivedPacket.packetID;
    }



    else if(receivedPacket.packetID < lastPacketID + 1){
      console.log("delayed packet received");
      console.log("packetID: " + receivedPacket.packetID)
      
      var json = {
        "type":"errorPacket",
        "errorType": "delayedPacket",
        "packetID":receivedPacket.packetID};
    
      server.send(JSON.stringify(json), sender.port, sender.address, (err) => {
        console.log(`Message sent to ${sender.address}:${sender.port}`)
      })
    }



    return;
  }

  lastPacketID = receivedPacket.packetID;



  var json = {
    "type":"serverToClientPingResponse",
    "packetID":receivedPacket.packetID,
    "clientToServerPing":clientToServerPing,
    "serverDatetime":datetime};

  server.send(JSON.stringify(json), sender.port, sender.address, (err) => {
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
