/// <reference path="jquery-2.1.3.js" />

var serverConfig = {
    servers: [],
    registerServer: function (readPort, writePort, serverLineColor) {
        var newServer = new Server(readPort, writePort, serverLineColor);
        newServer.initReadServiceListener();
        newServer.initWriteServiceListener();
        this.servers.push(newServer);
        return newServer;
    },
    sendDataForAllServers: function () {
        jQuery.each(this.servers, function (index, server) {
            if (server.readWSClient().readyState == WebSocket.OPEN) {
                var value = tools.getRandom(0, 30);
                server.readWSClient().send(value);
                tools.logToConsole("Sending data: " + value + " to read server...");
            }
        });
    },
    disconectAllChanels: function () {
        jQuery.each(this.servers, function (index, server) {
            var value = tools.getRandom(0, 30);
            server.readWSClient().close();
        });
    }
};

var tools = {
    getRandom: function (inf, sup) {
        pos = sup - inf
        mRan = Math.random() * pos
        mRan = Math.floor(mRan)
        return parseInt(inf) + mRan
    },
    logToConsole: function (logEntry) {
        console.log(logEntry);
    },
    getCanvasContext: function () {
        var canvasElement = document.getElementById('sumCanvas');
        var canvasContext = canvasElement.getContext('2d');
        return canvasContext;
    },
    printLine: function (x, y, lastX, lastY, color) {
        var context = this.getCanvasContext();
        context.font = "10px Arial";
        context.fillStyle = "#663366";
        context.strokeStyle = color;
        context.beginPath();
        var moveToIndex = (40 + (lastX * 14))
        if (moveToIndex > 860) {
            context.moveTo(40, 560 - (lastY * 10));
        } else {
            context.moveTo(40 + (lastX * 14), 560 - (lastY * 10));
        }
        context.lineTo(40 + (x * 14), 560 - (y * 10));
        context.stroke();
        context.fillText(y.toString(), 40 + (x * 14), 555 - (y * 10))
        if (x >= 59) {
            context.clearRect(0, 0, 900, 600);
            initCanvas();
        }
        var t = "x: " + x.toString();
        t += "</br>y: " + y.toString();
        t += "</br> last x:" + lastX;
        t += "</br> last y: " + lastY;
        t += "</br> color: " + color;
        jQuery('#console').html(t);
    }
}

var Server = function (readPort, writePort, serverLineColor) {
    var _readWSClient = null;
    var _writeWSClient = null;
    this.LastY = 0;
    this.LastX = 10;
    this.ReadPort = readPort;
    this.WritePort = writePort;
    this.ServerLineColor = serverLineColor;
    this.ReadURL = function () {
        return 'ws://localhost:' + this.ReadPort + '/SumServer/ReadData'
    };
    this.WriteURL = function () {
        return 'ws://localhost:' + this.WritePort + '/SumServer/WriteData'
    };

    this.initReadServiceListener = function () {
        if (_readWSClient == null) {
            _readWSClient = new WebSocket(this.ReadURL());
            this.configReadWS();
        }
    };
    this.initWriteServiceListener = function () {
        if (_writeWSClient == null) {
            _writeWSClient = new WebSocket(this.WriteURL());
            this.configWriteWS();
        }
    };

    this.readWSClient = function () {
        if (_readWSClient == null) {
            _readWSClient = new WebSocket(this.ReadURL());
            this.configReadWS();
        }
        return _readWSClient;
    };
    this.writeWSClient = function () {
        if (_writeWSClient == null) {
            _writeWSClient = new WebSocket(this.WriteURL());
            this.configWriteWS();
        }
        return _writeWSClient;
    };

    this.configReadWS = function () {
        var readP = this.ReadPort;
        _readWSClient.onopen = function () {
            tools.logToConsole("Port: " + readP + " is open.");
        };
        _readWSClient.onerror = function () {
            tools.logToConsole("Port: " + readP + " handled error!!!");
        };
        _readWSClient.onclose = function () {
            tools.logToConsole("Port: " + readP + " is disconected.");
        };
    };

    this.configWriteWS = function () {
        var writeP = this.WritePort;
        var srvColor = this.ServerLineColor;
        var lx = this.LastX;
        var ly = this.LastY;
        _writeWSClient.onmessage = function (sumData) {
            tools.logToConsole("Server in port: " + writeP + " says:" + sumData.data);
            var d = new Date();
            var x = d.getSeconds();
            tools.printLine((x > 60) ? 0 : x, sumData.data, lx, ly, srvColor);
            lx = (x >= 60) ? 0 : x;
            ly = sumData.data;
            console.log(sumData.data);
        };
        _writeWSClient.onopen = function () {
            _writeWSClient.send("Ready!!!");
            tools.logToConsole("Port: " + writeP + " is open.");
        };
        _writeWSClient.onerror = function () {
            tools.logToConsole("Port: " + writeP + " handled error!!!");
        };
    }

    this.get_lastCoords = function () {
        var vRet = {
            lastX: this.LastX,
            lastY: this.LastY
        }
        return vRet;
    };
}

//Event handlers

jQuery().ready(function () {
    jQuery('#cmdOpenServerConfigPanel').click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        jQuery("#serverConfigPanel").removeClass('hidden');
        jQuery.blockUI({ message: $('#serverConfigPanel') });
    });

    jQuery('#txtReadServerPort').change(function (e) {
        jQuery('#lblReadServerURI').html("Server Url: http://localhost:" + jQuery(this).val() + '/SumServer/ReadData');
    });

    jQuery('#txtWriteServerPort').change(function (e) {
        jQuery('#lblWriteServerURI').html("Server Url: http://localhost:" + jQuery(this).val() + '/SumServer/WriteData');
    });

    jQuery('#cmdAddServer').click(function (e) {
        e.preventDefault();
        e.stopPropagation();

        var readServerPortValidity = document.getElementById('txtReadServerPort').validity;
        if (!readServerPortValidity.valid) {
            alert('Sorry, the read server port is invalid');
            return;
        };
        var writeServerPortValidity = document.getElementById('txtWriteServerPort').validity;
        if (!writeServerPortValidity.valid) {
            alert('Sorry, the write server port is invalid');
            return;
        };
        var readServerPort = jQuery('#txtReadServerPort').val();
        var writeServerPort = jQuery('#txtWriteServerPort').val();
        var serverLineColor = jQuery('#serverLineColor').val();
        serverConfig.registerServer(readServerPort, writeServerPort, serverLineColor);

        jQuery('#txtReadServerPort').val(null);
        jQuery('#txtWriteServerPort').val(null);
        jQuery('#lblReadServerURI').html('');
        jQuery('#lblWriteServerURI').html('');

        jQuery("#serverConfigPanel").addClass('hidden');
        jQuery.unblockUI();
    });

    jQuery('#cmdDisconectChanels').click(function (e) {
        e.preventDefault();
        e.stopPropagation();
        serverConfig.disconectAllChanels();
    });
    jQuery(document).everyTime(500, function () {
        serverConfig.sendDataForAllServers();
    });
    initCanvas();
});

var initCanvas = function () {
    var context = tools.getCanvasContext();
    context.fillStyle = "blue";
    context.font = "bold 16px Arial";
    context.fillText("y Axis (numbers)", 5, 16);
    context.fillRect(30, 30, 2, 530)
    context.fillStyle = "red";
    context.fillRect(30, 558, 890, 2)
    context.fillText("x Axis(seconds)", 760, 548);
    buildSeries();
};

var buildSeries = function () {
    var context = tools.getCanvasContext();
    context.font = "10px Arial";
    //Build y series
    var y = 0;
    for (var i = 53; i > 0; i--) {
        y++;
        context.fillStyle = "#EBEBEB";
        context.fillRect(35, (30 + y * 10), 900, 1);
        var text = i.toString();
        if (text.length == 1)
            text = " " + text;
        text += '-'
        context.fillStyle = "gray";
        context.fillText(text, 10, (30 + y * 10));
    }

    //Build x series

    for (var s = 0; s <= 60; s++) {
        context.fillText(s.toString(), 40 + (s * 14), 570);
    }
}