var connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub")
    .configureLogging(signalR.LogLevel.None) // Suppress SignalR logs
    .build();

connection.start()
    .then(function () {
        console.log("✅ Notification channel connected"); // Optional custom log
    })
    .catch(function (err) {
        return console.error("❌ SignalR start failed:", err.toString());
    });

connection.on("OnConnected", function () {
    OnConnected();
});

function OnConnected() {
    var username = $('#hfUsername').val();
    if (username !== "") {
        connection.invoke("SaveUserConnection", username)
            .catch(function (err) {
                return console.error("❌ Failed to register user:", err.toString());
            });
    }
}

connection.on("ReceivedNotification", function (message, targetUrl) {
    ModernAlert.confirm({
        title: 'New Notification',
        text: message,
        icon: 'info',
        confirmText: 'VIEW',
        cancelText: 'DISMISS',
        showCancelButton: true
    }).then(result => {
        if (result.isConfirmed) {
            if (targetUrl && targetUrl !== 'null' && targetUrl !== '') {
                window.location.href = targetUrl;
            } else {
                window.location.href = '/User/Notification/Index';
            }
        }
    });
});