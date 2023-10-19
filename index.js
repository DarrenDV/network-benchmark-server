console.log('server starting....');
const dgram = require('node:dgram');
const server = dgram.createSocket('udp4');
const worker = require('worker_threads');

var oldTestID;
var currentTestID;


var isSendingPacketsForTest = false;

var C2Sarray = [];





server.on('error', (err) => {
  console.error(`server error:\n${err.stack}`);
  server.close();
});









server.on('message', (msg, sender) => {
  var receivedPacket = JSON.parse(msg);
  

  var datetime = (new Date()).getTime();
  var C2S = datetime - receivedPacket.clientDatetime;
  C2Sarray.push(C2S);

  
  if(receivedPacket.testID != currentTestID){ //make sure we are on the same test
    C2Sarray = [];
    C2Sarray.push(C2S);
    console.log("new test started");
    oldTestID = currentTestID;
    currentTestID = receivedPacket.testID;
    isSendingPacketsForTest = false;
    if(!isSendingPacketsForTest){
      sendPackets(receivedPacket.packetCount, receivedPacket, sender);
    }
  }

  if(receivedPacket.testID == oldTestID){ //ignore old test packets
    console.log("old test packet received");
    return;
  }
  




  

  



});











server.on('listening', () => {
  const address = server.address();
  console.log(`server listening ${address.address}:${address.port}`);
});






server.bind(41234);
//Prints: server listening 0.0.0.0:41234







function delay(time){
  return new Promise(resolve => {
    setTimeout(resolve, time);
  });
}







async function sendPackets(amount, receivedPacket, sender){

  isSendingPacketsForTest = true;

  for(var i = 0; i < amount; i++){

    var datetime = (new Date()).getTime();

    var json = {
      "packetID":i+1,
      "serverDatetime":datetime,
      "C2STimings":C2Sarray};

    server.send(JSON.stringify(json), sender.port, sender.address, (err) => {
      console.log(`Message sent to ${sender.address}:${sender.port}`)
    })
    
    
    
    console.log("----------------------------------");
    await delay(1000);
  } 

  isSendingPacketsForTest = false;
}











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
