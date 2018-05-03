var signalr = $.hubConnection();

signalr.start().done(function () {
    console.log("SignalR connected!");
});

function readNetworkInt(byteArray, offset = 0) {
    var i = byteArray[offset + 3];
    i += byteArray[offset + 2] * 256;
    i += byteArray[offset + 1] * 256 * 256; 
    i += byteArray[offset + 0] * 256 * 256 * 256;
    return i;
}

function Base64Encode(str, encoding = 'utf-8') {
    var bytes = new (TextEncoder || TextEncoderLite)(encoding).encode(str);
    return base64js.fromByteArray(bytes);
}

var lastKnownFrame = -1;

function load(done) {
    var oReq = new XMLHttpRequest();
    oReq.open("GET", `/api/ScreenCapture/${lastKnownFrame}`, true);
    oReq.responseType = "arraybuffer";

    oReq.onload = function (oEvent) {
        var arrayBuffer = oReq.response;
        var byteArray = new Uint8Array(arrayBuffer);

        console.log(`Received a ${byteArray.length} packet!`);

        var offset = 0;

        var canvas = document.getElementById("theCanvas");
        var ctx = canvas.getContext("2d");

        console.log(`Reading a 4 byets frame id`);
        if (offset + 4 >= byteArray.length) {
            console.log('not enough buffer');
            if (done != null) done();
            return;
        }

        lastKnownFrame = readNetworkInt(byteArray, offset);
        offset += 4;

        while (true) {
            console.log(`Reading a 12 byets image token`);

            if (offset + 12 >= byteArray.length) {
                console.log('not enough buffer');
                break;
            }

            var imageLength = readNetworkInt(byteArray, offset);
            offset += 4;

            var x = readNetworkInt(byteArray, offset);
            offset += 4;

            var y = readNetworkInt(byteArray, offset);
            offset += 4;

            console.log(`Reading a ${imageLength} bytes image at ${x}, ${y}`);

            if (offset + imageLength > byteArray.length) {
                console.log('not enough buffer');
                break;
            }

            // Obtain a blob: URL for the image data.
            var imageArray = byteArray.slice(offset, offset + imageLength);
            offset += imageLength;

            var encodedData = base64js.fromByteArray(imageArray);

            var img = new Image;

            var img = new Image();
            img.onload = function () {
                ctx.drawImage(this, x, y);
            }
            img.src = "data: image / png; base64, " + encodedData;
        }
        if (done != null) done();
    };

    oReq.send();
}

function singleRun(func) {
    var running = false;
    return function () {
        if (!running) {
            running = true;
            func();
            running = false;
        }
    }
}

var running = false;
window.setInterval(function () {
    if (!running) {
        running = true;
        load(function () { running = false });
    }
}, 100);