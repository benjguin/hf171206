WITH LastInWindow AS
(
SELECT
    deviceId,
    MAX(sendTime) AS maxSendTime,
    SYSTEM.Timestamp AS windowTime
    FROM [evthub] TIMESTAMP BY sendTime
    GROUP BY deviceId, TumblingWindow(second, 10)
)
SELECT 
    [evthub].deviceId, 
    [evthub].sendTime, 
    [evthub].measure1,
    [evthub].sendTimestamp, 
    LastInWindow.windowTime
INTO [evthub2]
FROM [evthub] TIMESTAMP BY sendTime
    INNER JOIN LastInWindow
    ON DATEDIFF(second, [evthub], LastInWindow) BETWEEN 0 AND 10
    AND [evthub].deviceID = LastInWindow.deviceId
    AND [evthub].sendTime = LastInWindow.maxSendTime
;