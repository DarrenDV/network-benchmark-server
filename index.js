console.log('server starting....');
const dgram = require('node:dgram');
const server = dgram.createSocket('udp4');
const createCsvWriter = require('csv-writer').createObjectCsvWriter;


const ntpClient = require('ntp-client');

let ntpTime = null;
let ntpGetTime = null;

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

// Call the function to fetch NTP time when your server starts
fetchNTPTime();

// You can then use ntpTime in your calculations
// For example, to calculate the current time with NTP offset in milliseconds
function getCurrentTime() {
  if (ntpTime) {
    // const now = new Date();
    // const offset = now - ntpTime;
    // return ntpTime.getTime() + offset;
    const diff = Date.now() - ntpGetTime;
    return ntpTime.getTime() + diff;
  } else {
    // Handle the case where NTP time hasn't been fetched yet
    return Date.now();
  }
}



var oldTestID;
var currentTestID;
var receivedPackets = 0;
var currentPacketLoss = 0;

const TIMEOUT_TIME = 10000; //ms
let timer;

var isSendingPacketsForTest = false;

var C2Sarray = [];
var incomingPacketData = [];





server.on('error', (err) => {
  console.error(`server error:\n${err.stack}`);
  server.close();
});



server.on('message', (msg, sender) => {
  
  var datetime = getCurrentTime();
  var receivedPacket = JSON.parse(msg);
  var C2S = datetime - receivedPacket.clientDatetime;
  // console.log("--------------------------------------------------");
  // console.log("clientDatetime: " + receivedPacket.clientDatetime);
  // console.log(`datetime: ${datetime}`);
  C2Sarray.push(C2S);

  //console.log(C2Sarray);

  clearTimeout(timer);


  receivedPackets++;

  currentPacketLoss = (receivedPackets / receivedPacket.packetID) * 100;

  
  if(receivedPacket.testID != currentTestID){ //make sure we are on the same test
    console.log("new test started");
    resetData();
    
    C2Sarray.push(C2S);
    receivedPackets++;
    currentPacketLoss = (receivedPackets / receivedPacket.packetID) * 100;
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
  
  //Handle incoming packet data
  incomingPacketData.push({
    packetID: receivedPacket.packetID,
    packetCount: receivedPacket.packetCount,
    C2S: C2S,
    currentPacketLoss: currentPacketLoss
  })

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