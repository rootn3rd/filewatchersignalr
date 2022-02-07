const startStreamingBtn = document.getElementById('btnStream');
const result = document.getElementById('result');
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/filemonitor")
    .build();

const filePath = "D:\\StudioWorks\\NETCoreProjects\\FileWatcherSignalR\\FileWatcherConsole\\data.txt";

connection.start()
    .then(() => {
        console.log("Connected!");

        connection.invoke("GetLastTenElements", filePath)
            .then(res => {
                console.log(res);
                res.forEach(r=> addElement(r));
                enableStreaming();

            });
        //connection.invoke("GetFileStat", filePath).then(function (res) {
        //    if (res) {
        //        enableStreaming();
        //        return;
        //    }

        //    startStreamingBtn.onclick = function () {
        //        enableStreaming();
        //    }
        //})

        //startStreamingBtn.onclick = function () {
        //    enableStreaming();
        //}
    })
    .catch(err => console.error(err));

const enableStreaming = () => {

    startStreamingBtn.setAttribute("disabled", "disabled");

    connection.stream("StreamContents", filePath)
        .subscribe({
            next: (data) => {
                addElement(data);
            },
            close: false,
            error: (err) => {
                console.error('Error: ', err);
            }
        });
}

const addElement = (element) => {
    let child = '<div class="data">' + element + '</div>';
    result.insertAdjacentHTML('beforeend', child);
    result.scrollTop = result.scrollHeight;
}