watcherBtn = document.getElementById('btnWatch');
streamResult = document.getElementById('streamResults');
btnListen = document.getElementById('btnListen');
listenerResults = document.getElementById('listenerResults');

let connection = new signalR.HubConnectionBuilder().withUrl("/watch").build();


connection.start().then(t => {

    // setupListener();

    watcherBtn.onclick = (e) => {
        setupStreaming();
    }

    //btnListen.onclick = (e) => {
    //    //connection.send('GetContents');
    //    setupListener();
    //}

});


const setupStreaming = () => {

    //connection.stream('watch').subscribe({
    //    close: false,
    //    next: (data) => {
    //        let child = '<div class="data">' + data + '</div>';
    //        streamResult.insertAdjacentHTML('beforeend', child);
    //    },
    //    error: (err) => console.error('Error : ', err)
    //});

    connection.stream('MultipleWatcher').subscribe({
        close: false,
        next: (data) => {
            let child = '<div class="data">' + data + '</div>';
            streamResult.insertAdjacentHTML('beforeend', child);
            streamResult.scrollTop = streamResult.scrollHeight;
        },
        error: (err) => console.error('Error : ', err)
    });


};

const setupListener = () => {

    connection.on('initialData', (dataArr) => {
        for (var data in dataArr) {

            let child = '<div class="data-listen-init">' + data + '</div>';
            listenerResults.insertAdjacentHTML('beforeend', child);
        }
    });

    connection.on('dataRead', (data) => {

        let child = '<div class="data-listen">' + data + '</div>';
        listenerResults.insertAdjacentHTML('beforeend', child);
        listenerResults.scrollTop = listenerResults.scrollHeight;
    });

};