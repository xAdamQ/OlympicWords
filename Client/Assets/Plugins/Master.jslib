mergeInto(LibraryManager.library, {

    Hello: function () {
        window.alert("Hello, world!");
    },

    HelloString: function (str) {
        window.alert(Pointer_stringify(str));
    },

    PrintFloatArray: function (array, size) {
        for (var i = 0; i < size; i++)
            console.log(HEAPF32[(array >> 2) + i]);
    },

    AddNumbers: function (x, y) {
        return x + y;
    },

    StringReturnValueFunction: function () {
        var returnStr = "bla";

        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);

        return buffer;
    },

    BindWebGLTexture: function (texture) {
        GLctx.bindTexture(GLctx.TEXTURE_2D, GL.textures[texture]);
    },

    GetUserData: function () {
        var ud = JSON.stringify({
            Token: token,
            EnteredBefore: enteredBefore,
            Name: FBInstant.player.getName(),
            PictureUrl: FBInstant.player.getPhoto(),
        });

        var bufferSize = lengthBytesUTF8(ud) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(ud, buffer, bufferSize);

        return buffer;
    },

    GetFriends: function () {
        var fs = JSON.stringify(friends);

        var bufferSize = lengthBytesUTF8(fs) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(fs, buffer, bufferSize);

        return buffer;
    },

    StartFbigGame: function () { FBInstant.startGameAsync().then(onGameStart) },

    IsFigSdkInit: function () { return fbigSdkInitialized },

    BackendAddress: function () { return backendAddress },
});