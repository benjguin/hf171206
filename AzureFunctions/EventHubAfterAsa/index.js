module.exports = function (context, eventHubMessages) {
    eventHubMessage = eventHubMessages[0];
    context.log(`JavaScript eventhub trigger function called for JSON message: ${JSON.stringify(eventHubMessage)}`);

    var timeKey = 9000000000000 - eventHubMessage.sendtimestamp; // 9000000000000 == Wednesday, March 14, 2255 4:00:00 PM
    context.log(timeKey);

    context.bindings.outputTable = {
        "partitionKey": eventHubMessage.deviceid,
        "rowKey": timeKey,
        "deviceId": eventHubMessage.deviceid,
        "measure1": eventHubMessage.measure1,
        "sendTime": eventHubMessage.sendtime,
        "sendTimestamp": eventHubMessage.sendtimestamp,
        "windowTime": eventHubMessage.windowtime,
    }


    context.done();
};