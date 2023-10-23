const dgram = require('node:dgram');
const createCsvWriter = require('csv-writer').createObjectCsvWriter;
const ntpClient = require('ntp-client');

const server = dgram.createSocket('udp4');

let ntpTime = null;
let ntpGetTime = null;

var oldTestID;
var currentTestID;
var receivedPackets = 0;
var currentPacketLoss = 0;

const TIMEOUT_TIME = 10000; //ms
let timer;

var C2Sarray = [];
var incomingPacketData = [];


// Function to fetch NTP time and store it
function fetchNTPTime() {
  ntpClient.getNetworkTime("pool.ntp.org", 123, (err, date) => {
    if (err) {
      console.error("Error fetching NTP time:", err);
    } else {
      ntpTime = date;
      ntpGetTime = Date.now();
      console.log("NTP time fetched:", ntpTime);
      console.log(ntpTime.getTime());
    }
  });
}

function getCurrentTime() {
  if (ntpTime) {
    const diff = Date.now() - ntpGetTime;
    return ntpTime.getTime() + diff;
  } else {
    // Handle the case where NTP time hasn't been fetched yet
    return Date.now();
  }
}

function onServerError(err){
  console.error(`server error:\n${err.stack}`);
  server.close();
}

function onMessage(msg, sender){
  var datetime = getCurrentTime();
  var receivedPacket = JSON.parse(msg);
  var C2S = datetime - receivedPacket.clientDatetime;
  
  OnPacketReceived(receivedPacket, C2S);
  
  clearTimeout(timer);
  
  if(receivedPacket.testID != currentTestID){ //make sure we are on the same test
    console.log("new test started");
    resetData();
    OnPacketReceived(receivedPacket, C2S);
    
    oldTestID = currentTestID;
    currentTestID = receivedPacket.testID;
    
    sendPackets(receivedPacket.packetCount, sender);
  }
  
  if(receivedPacket.testID == oldTestID){ //ignore old test packets
    console.log("old test packet received");
    return;
  }
  
  //Check if we received all packets
  if(receivedPackets >= receivedPacket.packetCount){
    clearTimeout(timer);
    saveData(receivedPacket.testID);
    return;
  }else{
    timer = setTimeout(() => {
      console.log(`No packets received for ${TIMEOUT_TIME / 1000} seconds.`);
      saveData(receivedPacket.testID);
      resetData();
    }, TIMEOUT_TIME);
  }
}

function OnPacketReceived(receivedPacket, C2S){
  receivedPackets++;
  currentPacketLoss = (receivedPackets / receivedPacket.packetID) * 100;
  C2Sarray.push(C2S);
  
  //Handle incoming packet data
  incomingPacketData.push({
    packetID: receivedPacket.packetID,
    packetCount: receivedPacket.packetCount,
    C2S: C2S,
    currentPacketLoss: currentPacketLoss
  })
  
}

function OnListen(){
  const address = server.address();
  console.log(`server listening ${address.address}:${address.port}`);
}

function delay(time){
  return new Promise(resolve => {
    setTimeout(resolve, time);
  });
}

async function sendPackets(amount, sender){
  
  isSendingPacketsForTest = true;
  
  for(var i = 0; i < amount; i++){
    
    var json = {
      "packetID":i+1,
      "serverDatetime":getCurrentTime(),
      "C2STimings":C2Sarray,
      "C2SSuccessRate":currentPacketLoss
    };
    
    server.send(JSON.stringify(json), sender.port, sender.address, (err) => {
      console.log(`Message sent to ${sender.address}:${sender.port}`)
    })
    
    console.log("----------------------------------");
    await delay(16);
  } 
  
  isSendingPacketsForTest = false;
}

function saveData(testID){
  const csvWriter = createCsvWriter({
    path: `${testID}-server.csv`, // Specify the path where you want to save the CSV file
    header: [
      { id: 'packetID', title: 'PacketID' },
      { id: 'packetCount', title: 'PacketCount' },
      { id: 'C2S', title: 'C2S' },
      { id: 'currentPacketLoss', title: 'CurrentPacketLoss' },
      
    ],
  });
  
  csvWriter
  .writeRecords(incomingPacketData)
  .then(() => console.log('The CSV file was written successfully'));
}

function resetData(){
  console.log("resetting data");
  C2Sarray = [];
  receivedPackets = 0;
  currentPacketLoss = 100;
  incomingPacketData = [];
}





console.log('server starting....');


fetchNTPTime();

server.on('error', onServerError);

server.on('listening', OnListen)

server.on('message', onMessage);

server.bind(41234);
//Prints: server listening 0.0.0.0:41234