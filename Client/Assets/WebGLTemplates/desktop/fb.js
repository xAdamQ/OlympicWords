let loadedUnityInstance = null;

//app config
const fbConfig = {
    appId: '468588098648394', cookie: true, xfbml: true, version: 'v15.0'
};

// load sdk
const script = "script";
const fbSdk = "facebook-jssdk";
let js, fjs = document.getElementsByTagName(script)[0];
if (!document.getElementById(fbSdk)) {
    js = document.createElement(script);
    js.id = fbSdk;
    js.src = "https://connect.facebook.net/en_US/sdk.js";
    fjs.parentNode.insertBefore(js, fjs);
    console.log("fb script loaded");
}


//init sdk
window.fbAsyncInit = function () {
    FB.init(fbConfig);
    FB.AppEvents.logPageView();
    console.log("fb init");
};

//the fb button uses this on the html file
function OnFbLogin() {
    FB.getLoginStatus(response => {

        console.log("the fb callback is: " + JSON.stringify(response));

        loadedUnityInstance.SendMessage("SignInPanel", "FbLogin", JSON.stringify(response));

        if (response.status === "connected") {
            document.getElementById("fbLoginButton").hidden = true;
        }
    });
}

//called from unity
function ShowFbButton() {
    document.getElementById("fbLoginButton").hidden = false;
}

function HideFbButton() {
    document.getElementById("fbLoginButton").hidden = true;
}



